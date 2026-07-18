var currentParams = [];

function selectFunction(functionId) {
    currentFunctionId = functionId;

    $('#functionTabs li').removeClass('active');
    $('#functionTabs li').has('a[onclick*="' + functionId + '"]').addClass('active');

    $.ajax({
        url: APP_ROOT + '/api/flow/parameters',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ functionId: functionId }),
        success: function(res) {
            if (res.success && res.data && res.data.length > 0) {
                currentParams = res.data;
                showParamModal(functionId);
            } else {
                currentParams = [];
                executeFunction(functionId, {});
            }
        }
    });
}

function executeFunction(functionId, params) {
    if (currentParams && currentParams.length > 0 && (!params || Object.keys(params).length === 0)) {
        showParamModal(functionId);
        return;
    }

    $('#resultArea').html('<div class="loading">正在查询</div>');

    $.ajax({
        url: APP_ROOT + '/api/flow/execute',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ functionId: functionId, params: params || {} }),
        success: function(res) {
            if (res.success && res.data) {
                renderResults(res.data);
                $('#queryTime').text(res.data.elapsedMs ? '执行: ' + res.data.elapsedMs + 'ms' : '');
            } else {
                $('#resultArea').html(
                    '<div class="alert alert-danger">' + (res.message || '执行失败') + '</div>'
                );
            }
        },
        error: function() {
            $('#resultArea').html(
                '<div class="alert alert-danger">网络错误，请稍后重试</div>'
            );
        }
    });
}

function showParamModal(functionId) {
    var body = $('#paramBody').empty();

    $.each(currentParams, function(i, p) {
        var required = p.isRequired ? ' <span class="text-danger">*</span>' : '';
        var paramId = 'pf_' + i;
        var html = '<div class="form-group">' +
                   '<label>' + p.label + required + '</label>';

        if (p.controlType === 'ComboBox' && p.dataSource) {
            html += '<select class="form-control" id="' + paramId + '">';
            $.each(p.dataSource, function(j, opt) {
                var selected = (opt === p.defaultValue) ? ' selected' : '';
                html += '<option value="' + opt + '"' + selected + '>' + opt + '</option>';
            });
            html += '</select>';
            html += '<input type="hidden" class="param-name" value="' + p.name + '" />';
        } else if (p.controlType === 'DateTimePicker') {
            var today = new Date().toISOString().slice(0, 10);
            html += '<input type="date" class="form-control" id="' + paramId + '" value="' + (p.defaultValue || today) + '" />';
            html += '<input type="hidden" class="param-name" value="' + p.name + '" />';
        } else {
            html += '<input type="text" class="form-control" id="' + paramId + '" value="' + (p.defaultValue || '') + '" placeholder="请输入' + p.label + '" />';
            html += '<input type="hidden" class="param-name" value="' + p.name + '" />';
        }

        html += '</div>';
        body.append(html);
    });

    $('#paramModal .modal-title').text('查询参数');
    $('#paramModal .modal-footer').html(
        '<button type="button" class="btn btn-default" data-dismiss="modal">取消</button>' +
        '<button type="button" class="btn btn-primary" onclick="submitParams()">' +
        '<span class="glyphicon glyphicon-ok"></span> 确认</button>'
    );

    $('#paramModal').data('functionId', functionId).modal('show');
}

function submitParams() {
    var functionId = $('#paramModal').data('functionId');
    var params = {};
    var valid = true;

    $('#paramBody .form-group').each(function(i) {
        var input = $(this).find('select, input[type!=hidden]');
        var nameInput = $(this).find('.param-name');
        var p = currentParams[i];
        if (!input.length || !nameInput.length) return;

        var name = nameInput.val();
        var val = input.val();
        if (val !== undefined && val !== null) {
            params[name] = val.trim();
        }
        if (p && p.isRequired && (!params[name])) {
            valid = false;
            alert('请填写: ' + p.label);
        }
    });

    if (!valid) return;

    $('#paramModal').modal('hide');
    executeFunction(functionId, params);
}

function renderResults(data) {
    if (!data || !data.blocks) {
        $('#resultArea').html('<div class="alert alert-info">暂无数据</div>');
        return;
    }

    var html = '';

    $.each(data.blocks, function(i, block) {
        html += '<div class="result-section">';
        html += '<h4 class="section-title">' + (block.title || '结果') + '</h4>';

        if (block.type === 'table' && block.tableData) {
            html += '<div class="table-block">';
            html += '<table class="table table-striped table-bordered table-hover table-result">';
            html += '<thead><tr>';
            $.each(block.tableData.columns, function(j, col) {
                html += '<th>' + col + '</th>';
            });
            html += '</tr></thead><tbody>';
            $.each(block.tableData.rows, function(j, row) {
                html += '<tr>';
                $.each(block.tableData.columns, function(k, col) {
                    var val = row[col];
                    html += '<td>' + (val !== null && val !== undefined ? val : '') + '</td>';
                });
                html += '</tr>';
            });
            html += '</tbody></table>';
            html += '</div>';
        } else if (block.type === 'text') {
            html += '<div class="text-block">' + (block.textContent || '') + '</div>';
        } else if (block.type === 'error') {
            html += '<div class="error-block">' +
                    '<div class="alert alert-warning">' +
                    '<span class="glyphicon glyphicon-warning-sign"></span> ' +
                    (block.textContent || '') +
                    '</div></div>';
        }

        html += '</div>';
    });

    if (html === '') {
        html = '<div class="alert alert-info">执行完成，无展示内容</div>';
    }

    $('#resultArea').html(html);
}

$(document).on('keydown', '#searchInput', function(e) {
    if (e.keyCode === 13) doSearch();
});
