var currentEditType = '';
var currentEditId = 0;
var navStack = [];

function openConfig() {
    $('#configModal').modal('show');
    $('#configDetail').html('<p class="text-muted" style="margin-top:80px;text-align:center;">请从左侧选择配置项</p>');
    loadIntentList();
}

function loadIntentList(keyword) {
    $('#configTree').html('<div style="padding:20px;color:#999;">加载中...</div>');
    $.ajax({
        url: APP_ROOT + '/api/intent/list',
        success: function(res) {
            if (!res.success || !res.data) {
                $('#configTree').html('<div class="alert alert-danger">加载失败</div>');
                return;
            }
            var list = res.data;
            if (keyword) {
                var kw = keyword.toLowerCase();
                list = list.filter(function(i) {
                    return (i.name || '').toLowerCase().indexOf(kw) >= 0 ||
                           (i.category || '').toLowerCase().indexOf(kw) >= 0;
                });
            }
            var html = '';
            $.each(list, function(i, intent) {
                html += '<div class="config-tree-item" onclick="loadIntentDetail(' + intent.id + ')" data-id="intent-' + intent.id + '">';
                html += '📂 ' + escHtml(intent.name);
                html += ' <span class="label label-info" style="font-size:10px;">' + escHtml(intent.category || '') + '</span></div>';
            });
            $('#configTree').html(html || '<div style="padding:20px;color:#999;">暂无意图</div>');
        },
        error: function() {
            $('#configTree').html('<div class="alert alert-danger">网络错误</div>');
        }
    });
}

$(document).on('keyup', '#configSearch', function() {
    var kw = $(this).val().trim();
    loadIntentList(kw);
});

function loadIntentDetail(id) {
    highlightTreeItem('intent-' + id);
    showIntentDetail(id);
}

function showIntentDetail(id) {
    $.ajax({
        url: APP_ROOT + '/api/config/detail', data: { type: 'intent', id: id },
        success: function(res) {
            if (res.success) renderIntentDetail(res.data);
        }
    });
}

function loadFunctionDetail(id) {
    highlightTreeItem('func-' + id);
    showFunctionDetail(id);
}

function showFunctionDetail(id) {
    $.ajax({
        url: APP_ROOT + '/api/config/detail', data: { type: 'function', id: id },
        success: function(res) {
            if (res.success) renderFunctionDetail(res.data);
        }
    });
}

function showStepDetail(id) {
    highlightTreeItem('step-' + id);
    $.ajax({
        url: APP_ROOT + '/api/config/detail', data: { type: 'step', id: id },
        success: function(res) {
            if (!res.success) return;
            var s = res.data;
            navStack.push({ type: 'step', id: s.id, title: 'Step-' + s.sortOrder + ': ' + (s.label || s.stepType) });
            renderStepDetail(s);
        }
    });
}

function showParamDetail(id) {
    highlightTreeItem('param-' + id);
    $.ajax({
        url: APP_ROOT + '/api/config/detail', data: { type: 'parameter', id: id },
        success: function(res) {
            if (!res.success) return;
            var p = res.data;
            navStack.push({ type: 'param', id: p.id, title: p.label || p.name });
            renderParamDetail(p);
        }
    });
}

function highlightTreeItem(id) {
    $('.config-tree-item').removeClass('active');
    $('[data-id="' + id + '"]').addClass('active');
}

function renderBreadcrumb() {
    var h = '<div style="padding:5px 0;margin-bottom:10px;border-bottom:1px solid #eee;font-size:13px;">';
    h += '<a href="javascript:void(0)" onclick="goBackToRoot()" style="color:#337ab7;">配置</a>';
    for (var i = 0; i < navStack.length; i++) {
        var n = navStack[i];
        h += ' <span style="color:#ccc;">/</span> ';
        if (i === navStack.length - 1) {
            h += '<strong>' + escHtml(n.title) + '</strong>';
        } else {
            h += '<a href="javascript:void(0)" onclick="goBackTo(' + (i + 1) + ')" style="color:#337ab7;">' + escHtml(n.title) + '</a>';
        }
    }
    h += '</div>';
    return h;
}
function goBackToRoot() { navStack = []; $('#configDetail').html('<p class="text-muted" style="margin-top:80px;text-align:center;">请从左侧选择配置项</p>'); }
function goBackTo(idx) {
    navStack = navStack.slice(0, idx);
    var last = navStack[navStack.length - 1];
    if (last.type === 'intent') showIntentDetail(last.id);
    else if (last.type === 'function') loadFunctionDetail(last.id);
    else { goBackToRoot(); }
}

function renderIntentDetail(data) {
    navStack = [{ type: 'intent', id: data.id, title: data.name }];
    $.ajax({
        url: APP_ROOT + '/api/flow/functions',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ intentId: data.id }),
        success: function(funcRes) {
            var funcs = (funcRes.success && funcRes.data) ? funcRes.data : [];
            var h = renderBreadcrumb();
            h += '<div class="detail-section"><h5>基本信息 <button class="btn btn-xs btn-primary pull-right" onclick="editIntent(' + data.id + ')">编辑</button></h5>';
            h += '<div class="detail-field"><label>名称</label><div class="value">' + escHtml(data.name) + '</div></div>';
            h += '<div class="detail-field"><label>描述</label><div class="value">' + escHtml(data.description) + '</div></div>';
            h += '<div class="detail-field"><label>关键词</label><div class="value">' + escHtml(data.keywords) + '</div></div>';
            h += '<div class="detail-field"><label>分类</label><div class="value">' + escHtml(data.category) + '</div></div>';
            h += '<div class="detail-field"><label>状态</label><div class="value">' + (data.isActive ? '启用' : '停用') + '</div></div></div>';
            h += '<button class="btn btn-sm btn-success" onclick="addFunction(' + data.id + ')">+ 新增 Function</button> ';
            h += '<button class="btn btn-sm btn-danger" onclick="deleteItem(\'intent\',' + data.id + ')">删除</button>';
            h += '<hr/><h5>功能列表</h5>';
            if (funcs.length === 0) {
                h += '<p style="color:#999;">暂无功能</p>';
            } else {
                h += '<div class="list-group">';
                $.each(funcs, function(i, f) {
                    h += '<a class="list-group-item" onclick="loadFunctionDetail(' + f.id + ')" style="cursor:pointer;">';
                    h += '<strong>' + escHtml(f.name) + '</strong>';
                    h += ' <span class="label label-default">' + escHtml(f.dataSource || 'Config') + '</span>';
                    if (f.description) h += '<br/><small>' + escHtml(f.description) + '</small>';
                    h += '</a>';
                });
                h += '</div>';
            }
            $('#configDetail').html(h);
        }
    });
}

function renderFunctionDetail(data) {
    if (navStack.length === 0 || navStack[navStack.length - 1].type !== 'function') {
        navStack.push({ type: 'function', id: data.id, title: data.name });
    }
    var h = renderBreadcrumb();
    h += '<div class="detail-section"><h5>功能详情 <button class="btn btn-xs btn-primary pull-right" onclick="editFunction(' + data.id + ')">编辑</button></h5>';
    h += '<div class="detail-field"><label>名称</label><div class="value">' + escHtml(data.name) + '</div></div>';
    h += '<div class="detail-field"><label>数据源</label><div class="value">' + escHtml(data.dataSource || 'Config') + '</div></div></div>';
    h += '<button class="btn btn-sm btn-success" onclick="addStep(' + data.id + ')">+ 新增 Step</button> ';
    h += '<button class="btn btn-sm btn-info" onclick="addParam(' + data.id + ')">+ 新增参数</button> ';
    h += '<button class="btn btn-sm btn-danger" onclick="deleteItem(\'function\',' + data.id + ')">删除</button>';

    var stepUrl = APP_ROOT + '/api/config/detail';
    $.ajax({
        url: stepUrl, data: { type: 'step', id: 0 },
        success: function() {}
    });

    $.when(
        $.ajax({ url: APP_ROOT + '/api/flow/parameters', method: 'POST', contentType: 'application/json', data: JSON.stringify({ functionId: data.id }) }),
        $.ajax({ url: APP_ROOT + '/api/config/tree' })
    ).then(function(paramRes, treeRes) {
        var params = (paramRes[0].success && paramRes[0].data) ? paramRes[0].data : [];
        var intents = treeRes[0].data || [];
        var steps = [];
        for (var i = 0; i < intents.length; i++) {
            var funcs = intents[i].functions || [];
            for (var j = 0; j < funcs.length; j++) {
                if (funcs[j].id === data.id) {
                    steps = funcs[j].steps || [];
                    break;
                }
            }
            if (steps.length > 0) break;
        }

        if (steps.length > 0) {
            h += '<hr/><h5>执行流</h5><div class="list-group">';
            $.each(steps, function(k, s) {
                var icon = s.stepType === 'sql' ? '🔷' : (s.stepType === 'end' ? '⏹️' : '📄');
                h += '<a class="list-group-item" onclick="showStepDetail(' + s.id + ')" style="cursor:pointer;">';
                h += icon + ' Step-' + s.sortOrder + ': ' + escHtml(s.label || s.stepType);
                if (s.stepType === 'sql') h += ' <span class="label label-info">sql</span>';
                h += '</a>';
            });
            h += '</div>';
        }
        if (params.length > 0) {
            h += '<hr/><h5>参数</h5><div class="list-group">';
            $.each(params, function(k, p) {
                h += '<div class="list-group-item" style="display:flex;justify-content:space-between;align-items:center;">';
                h += '<span onclick="showParamDetail(' + p.id + ')" style="cursor:pointer;flex:1;">';
                h += '📌 ' + escHtml(p.label) + ' (' + p.name + ')';
                h += ' <span class="label label-default">' + p.controlType + '</span>';
                if (p.isRequired) h += ' <span class="label label-danger">必填</span>';
                h += '</span>';
                h += '<button class="btn btn-xs btn-primary" onclick="editParam(' + p.id + ')">编辑</button>';
                h += '</div>';
            });
            h += '</div>';
        }
        $('#configDetail').html(h);
    });
}

function renderStepDetail(data) {
    var h = renderBreadcrumb();
    h += '<div class="detail-section"><h5>步骤详情 <button class="btn btn-xs btn-primary pull-right" onclick="editStep(' + data.id + ')">编辑</button></h5>';
    h += '<div class="detail-field"><label>类型</label><div class="value">' + data.stepType + '</div></div>';
    h += '<div class="detail-field"><label>排序</label><div class="value">' + data.sortOrder + '</div></div>';
    h += '<div class="detail-field"><label>标签</label><div class="value">' + escHtml(data.label) + '</div></div>';
    if (data.stepType === 'sql') {
        h += '<div class="detail-field"><label>SQL</label><div class="value"><pre>' + escHtml(data.sqlText) + '</pre></div></div>';
        h += '<div class="detail-field"><label>结果变量</label><div class="value">' + escHtml(data.resultVar) + '</div></div>';
        if (data.expectOperator) {
            h += '<div class="detail-field"><label>预期</label><div class="value">' + data.expectOperator + ' ' + data.expectValue;
            if (data.expectOnFail) h += ' -> ' + data.expectOnFail + (data.expectTarget !== null ? ' Step-' + data.expectTarget : '');
            h += '</div></div>';
            if (data.expectMessage) h += '<div class="detail-field"><label>诊断</label><div class="value"><pre>' + escHtml(data.expectMessage) + '</pre></div></div>';
        }
    } else {
        h += '<div class="detail-field"><label>展示标题</label><div class="value">' + escHtml(data.displayTitle) + '</div></div>';
        h += '<div class="detail-field"><label>变量</label><div class="value">' + escHtml(data.displaySource) + '</div></div>';
    }
    h += '</div><button class="btn btn-sm btn-danger" onclick="deleteItem(\'step\',' + data.id + ')">删除</button>';
    $('#configDetail').html(h);
}

function renderParamDetail(data) {
    var h = renderBreadcrumb();
    h += '<div class="detail-section"><h5>参数详情 <button class="btn btn-xs btn-primary pull-right" onclick="editParam(' + data.id + ')">编辑</button></h5>';
    h += '<div class="detail-field"><label>参数名</label><div class="value">' + escHtml(data.name) + '</div></div>';
    h += '<div class="detail-field"><label>标签</label><div class="value">' + escHtml(data.label) + '</div></div>';
    h += '<div class="detail-field"><label>控件</label><div class="value">' + data.controlType + '</div></div>';
    h += '<div class="detail-field"><label>必填</label><div class="value">' + (data.isRequired ? '是' : '否') + '</div></div></div>';
    h += '<button class="btn btn-sm btn-danger" onclick="deleteItem(\'parameter\',' + data.id + ')">删除</button>';
    $('#configDetail').html(h);
}

function editIntent(id) {
    currentEditType = 'intent'; currentEditId = id;
    $('#editModal').data('etype', 'intent').data('eid', id);
    $.ajax({
        url: APP_ROOT + '/api/config/detail', data: { type: 'intent', id: id },
        success: function(res) {
            if (!res.success) return;
            var d = res.data;
            var h = '<div class="form-group"><label>名称</label><input class="form-control" id="edit_name" value="' + escAttr(d.name) + '" /></div>';
            h += '<div class="form-group"><label>描述</label><textarea class="form-control" id="edit_desc" rows="2">' + escAttr(d.description) + '</textarea></div>';
            h += '<div class="form-group"><label>关键词</label><input class="form-control" id="edit_keywords" value="' + escAttr(d.keywords) + '" /></div>';
            h += '<div class="form-group"><label>分类</label><input class="form-control" id="edit_category" value="' + escAttr(d.category) + '" /></div>';
            h += '<div class="checkbox"><label><input type="checkbox" id="edit_active"' + (d.isActive ? ' checked' : '') + ' /> 启用</label></div>';
            showEditModal('编辑意图', h);
        }
    });
}
function editFunction(id) {
    currentEditType = 'function'; currentEditId = id;
    $('#editModal').data('etype', 'function').data('eid', id);
    $.ajax({
        url: APP_ROOT + '/api/config/detail', data: { type: 'function', id: id },
        success: function(res) {
            if (!res.success) return;
            var d = res.data;
            var h = '<div class="form-group"><label>名称</label><input class="form-control" id="edit_name" value="' + escAttr(d.name) + '" /></div>';
            h += '<div class="form-group"><label>描述</label><textarea class="form-control" id="edit_desc" rows="2">' + escAttr(d.description) + '</textarea></div>';
            h += '<div class="form-group"><label>排序</label><input type="number" class="form-control" id="edit_sortOrder" value="' + d.sortOrder + '" /></div>';
            h += '<div class="form-group"><label>数据源</label><select class="form-control" id="edit_dataSource">';
            h += '<option value="Config"' + (d.dataSource === 'Config' || !d.dataSource ? ' selected' : '') + '>Config (配置库)</option>';
            h += '<option value="BusinessDB"' + (d.dataSource === 'BusinessDB' ? ' selected' : '') + '>BusinessDB (业务库)</option></select></div>';
            showEditModal('编辑功能', h);
        }
    });
}
function editStep(id) {
    currentEditType = 'step'; currentEditId = id;
    $('#editModal').data('etype', 'step').data('eid', id);
    $.ajax({
        url: APP_ROOT + '/api/config/detail', data: { type: 'step', id: id },
        success: function(res) {
            if (!res.success) return;
            var d = res.data;
            var h = '<div class="form-group"><label>类型</label><select class="form-control" id="edit_stepType">';
            h += '<option value="sql"' + (d.stepType === 'sql' ? ' selected' : '') + '>sql</option>';
            h += '<option value="show_table"' + (d.stepType === 'show_table' ? ' selected' : '') + '>show_table</option>';
            h += '<option value="show_text"' + (d.stepType === 'show_text' ? ' selected' : '') + '>show_text</option>';
            h += '<option value="show_error"' + (d.stepType === 'show_error' ? ' selected' : '') + '>show_error</option>';
            h += '<option value="end"' + (d.stepType === 'end' ? ' selected' : '') + '>end</option></select></div>';
            h += '<div class="form-group"><label>排序</label><input type="number" class="form-control" id="edit_sortOrder" value="' + d.sortOrder + '" /></div>';
            h += '<div class="form-group"><label>标签</label><input class="form-control" id="edit_label" value="' + escAttr(d.label) + '" /></div>';
            h += '<div class="form-group"><label>SQL</label><textarea class="form-control" id="edit_sql" rows="4">' + escAttr(d.sqlText) + '</textarea></div>';
            h += '<div class="form-group"><label>结果变量</label><input class="form-control" id="edit_resultVar" value="' + escAttr(d.resultVar) + '" /></div>';
            h += '<h5>预期检查</h5>';
            h += '<div class="form-group"><label>操作符</label><select class="form-control" id="edit_expOpr">';
            h += '<option value="">无</option><option value="gt"' + (d.expectOperator === 'gt' ? ' selected' : '') + '>大于</option>';
            h += '<option value="eq"' + (d.expectOperator === 'eq' ? ' selected' : '') + '>等于</option>';
            h += '<option value="lt"' + (d.expectOperator === 'lt' ? ' selected' : '') + '>小于</option></select></div>';
            h += '<div class="form-group"><label>预期值</label><input class="form-control" id="edit_expVal" value="' + escAttr(d.expectValue) + '" /></div>';
            h += '<div class="form-group"><label>失败处理</label><select class="form-control" id="edit_expFail">';
            h += '<option value="">无</option><option value="goto"' + (d.expectOnFail === 'goto' ? ' selected' : '') + '>goto跳转</option>';
            h += '<option value="show_error"' + (d.expectOnFail === 'show_error' ? ' selected' : '') + '>show_error</option>';
            h += '<option value="stop"' + (d.expectOnFail === 'stop' ? ' selected' : '') + '>stop终止</option></select></div>';
            h += '<div class="form-group"><label>跳转目标</label><input type="number" class="form-control" id="edit_expTarget" value="' + (d.expectTarget !== null ? d.expectTarget : '') + '" /></div>';
            h += '<div class="form-group"><label>诊断信息</label><textarea class="form-control" id="edit_expMsg" rows="2">' + escAttr(d.expectMessage) + '</textarea></div>';
            h += '<h5>展示</h5>';
            h += '<div class="form-group"><label>展示标题</label><input class="form-control" id="edit_dispTitle" value="' + escAttr(d.displayTitle) + '" /></div>';
            h += '<div class="form-group"><label>变量</label><input class="form-control" id="edit_dispSrc" value="' + escAttr(d.displaySource) + '" /></div>';
            h += '<div class="form-group"><label>配置JSON</label><textarea class="form-control" id="edit_dispCfg" rows="2">' + escAttr(d.displayConfig) + '</textarea></div>';
            showEditModal('编辑步骤', h);
        }
    });
}
function editParam(id) {
    currentEditType = 'parameter'; currentEditId = id;
    $('#editModal').data('etype', 'parameter').data('eid', id);
    $.ajax({
        url: APP_ROOT + '/api/config/detail', data: { type: 'parameter', id: id },
        success: function(res) {
            if (!res.success) return;
            var d = res.data;
            var h = '<div class="form-group"><label>参数名</label><input class="form-control" id="edit_name" value="' + escAttr(d.name) + '" /></div>';
            h += '<div class="form-group"><label>标签</label><input class="form-control" id="edit_label" value="' + escAttr(d.label) + '" /></div>';
            h += '<div class="form-group"><label>控件类型</label><select class="form-control" id="edit_controlType">';
            h += '<option value="TextBox"' + (d.controlType === 'TextBox' ? ' selected' : '') + '>TextBox</option>';
            h += '<option value="ComboBox"' + (d.controlType === 'ComboBox' ? ' selected' : '') + '>ComboBox</option>';
            h += '<option value="DateTimePicker"' + (d.controlType === 'DateTimePicker' ? ' selected' : '') + '>DateTimePicker</option></select></div>';
            h += '<div class="form-group"><label>数据源JSON</label><textarea class="form-control" id="edit_dataSource" rows="2">' + escAttr(d.dataSource) + '</textarea></div>';
            h += '<div class="form-group"><label>默认值</label><input class="form-control" id="edit_defaultValue" value="' + escAttr(d.defaultValue) + '" /></div>';
            h += '<div class="checkbox"><label><input type="checkbox" id="edit_required"' + (d.isRequired ? ' checked' : '') + ' /> 必填</label></div>';
            showEditModal('编辑参数', h);
        }
    });
}
function showEditModal(title, content) {
    $('#editModalTitle').text(title);
    $('#editModalBody').html(content);
    $('#editModal .modal-footer').html(
        '<button type="button" class="btn btn-default" data-dismiss="modal">取消</button>' +
        '<button type="button" class="btn btn-primary" onclick="saveEdit()"><span class="glyphicon glyphicon-floppy-disk"></span> 保存</button>'
    );
    $('#editModal').modal('show');
}

function saveEdit() {
    var etype = $('#editModal').data('etype') || currentEditType;
    var eid = $('#editModal').data('eid') || currentEditId || 0;
    var data = {};
    if (etype === 'intent') {
        data.name = $('#edit_name').val();
        data.description = $('#edit_desc').val();
        data.keywords = $('#edit_keywords').val();
        data.category = $('#edit_category').val();
        data.isActive = $('#edit_active').is(':checked');
    } else if (etype === 'function') {
        data.name = $('#edit_name').val();
        data.description = $('#edit_desc').val();
        data.sortOrder = parseInt($('#edit_sortOrder').val()) || 0;
        data.dataSource = $('#edit_dataSource').val() || 'Config';
        if (eid === 0) { var iid = $('#edit_intentId').val(); if (iid) data.intentId = parseInt(iid); }
    } else if (etype === 'parameter') {
        data.name = $('#edit_name').val();
        data.label = $('#edit_label').val();
        data.controlType = $('#edit_controlType').val();
        data.dataSource = $('#edit_dataSource').val();
        data.defaultValue = $('#edit_defaultValue').val();
        data.isRequired = $('#edit_required').is(':checked');
        if (eid === 0) { var fid = $('#edit_functionId').val(); if (fid) data.functionId = parseInt(fid); }
    } else if (etype === 'step') {
        data.stepType = $('#edit_stepType').val();
        data.sortOrder = parseInt($('#edit_sortOrder').val()) || 0;
        data.label = $('#edit_label').val();
        data.sqlText = $('#edit_sql').val();
        data.resultVar = $('#edit_resultVar').val();
        data.expectOperator = $('#edit_expOpr').val();
        data.expectValue = $('#edit_expVal').val();
        data.expectOnFail = $('#edit_expFail').val();
        data.expectTarget = $('#edit_expTarget').val() ? parseInt($('#edit_expTarget').val()) : null;
        data.expectMessage = $('#edit_expMsg').val();
        data.displayTitle = $('#edit_dispTitle').val();
        data.displaySource = $('#edit_dispSrc').val();
        data.displayConfig = $('#edit_dispCfg').val();
        if (eid === 0) { var fid = $('#edit_functionId').val(); if (fid) data.functionId = parseInt(fid); }
    }
    if (eid > 0) data.id = eid;
    $.ajax({ url: APP_ROOT + '/api/config/save', method: 'POST', contentType: 'application/json', data: JSON.stringify({ type: etype, data: data }),
        success: function(res) {
            if (res.success) {
                $('#editModal').modal('hide');
                loadIntentList();
                if (etype === 'intent' && eid > 0) showIntentDetail(eid);
                else if (etype === 'function' && eid > 0) loadFunctionDetail(eid);
                else if (etype === 'step' && res.data && res.data.id) showStepDetail(res.data.id);
                else if (etype === 'parameter' && res.data && res.data.id) showParamDetail(res.data.id);
            } else alert('保存失败: ' + res.message);
        }, error: function() { alert('网络错误'); }
    });
}

function addIntent() {
    currentEditType = 'intent'; currentEditId = 0;
    $('#editModal').data('etype', 'intent').data('eid', 0);
    var h = '<div class="form-group"><label>名称</label><input class="form-control" id="edit_name" /></div>';
    h += '<div class="form-group"><label>描述</label><textarea class="form-control" id="edit_desc" rows="2"></textarea></div>';
    h += '<div class="form-group"><label>关键词</label><input class="form-control" id="edit_keywords" /></div>';
    h += '<div class="form-group"><label>分类</label><input class="form-control" id="edit_category" /></div>';
    h += '<div class="checkbox"><label><input type="checkbox" id="edit_active" checked /> 启用</label></div>';
    showEditModal('新增意图', h);
}
function addFunction(intentId) {
    currentEditType = 'function'; currentEditId = 0;
    $('#editModal').data('etype', 'function').data('eid', 0);
    var h = '<div class="form-group"><label>名称</label><input class="form-control" id="edit_name" /></div>';
    h += '<div class="form-group"><label>描述</label><textarea class="form-control" id="edit_desc" rows="2"></textarea></div>';
    h += '<div class="form-group"><label>排序</label><input type="number" class="form-control" id="edit_sortOrder" value="0" /></div>';
    h += '<div class="form-group"><label>数据源</label><select class="form-control" id="edit_dataSource"><option value="Config">Config (配置库)</option><option value="BusinessDB">BusinessDB (业务库)</option></select></div>';
    showEditModal('新增 Function', h);
    $('#editModalBody').append('<input type="hidden" id="edit_intentId" value="' + intentId + '" />');
}
function addStep(functionId) {
    currentEditType = 'step'; currentEditId = 0;
    $('#editModal').data('etype', 'step').data('eid', 0);
    var h = '<div class="form-group"><label>类型</label><select class="form-control" id="edit_stepType">';
    h += '<option value="sql">sql</option><option value="show_table">show_table</option>';
    h += '<option value="show_text">show_text</option><option value="show_error">show_error</option><option value="end">end</option></select></div>';
    h += '<div class="form-group"><label>排序</label><input type="number" class="form-control" id="edit_sortOrder" value="0" /></div>';
    h += '<div class="form-group"><label>标签</label><input class="form-control" id="edit_label" /></div>';
    h += '<div class="form-group"><label>SQL</label><textarea class="form-control" id="edit_sql" rows="4"></textarea></div>';
    h += '<div class="form-group"><label>结果变量</label><input class="form-control" id="edit_resultVar" /></div>';
    h += '<h5>预期检查</h5>';
    h += '<div class="form-group"><label>操作符</label><select class="form-control" id="edit_expOpr">';
    h += '<option value="">无</option><option value="gt">大于</option>';
    h += '<option value="eq">等于</option>';
    h += '<option value="lt">小于</option></select></div>';
    h += '<div class="form-group"><label>预期值</label><input class="form-control" id="edit_expVal" value="" /></div>';
    h += '<div class="form-group"><label>失败处理</label><select class="form-control" id="edit_expFail">';
    h += '<option value="">无</option><option value="goto">goto跳转</option>';
    h += '<option value="show_error">show_error</option>';
    h += '<option value="stop">stop终止</option></select></div>';
    h += '<div class="form-group"><label>跳转目标</label><input type="number" class="form-control" id="edit_expTarget" value="" /></div>';
    h += '<div class="form-group"><label>诊断信息</label><textarea class="form-control" id="edit_expMsg" rows="2"></textarea></div>';
    h += '<h5>展示</h5>';
    h += '<div class="form-group"><label>展示标题</label><input class="form-control" id="edit_dispTitle" value="" /></div>';
    h += '<div class="form-group"><label>变量</label><input class="form-control" id="edit_dispSrc" value="" /></div>';
    h += '<div class="form-group"><label>配置JSON</label><textarea class="form-control" id="edit_dispCfg" rows="2">{"columns":[]}</textarea></div>';
    showEditModal('新增步骤', h);
    $('#editModalBody').append('<input type="hidden" id="edit_functionId" value="' + functionId + '" />');
}
function addParam(functionId) {
    currentEditType = 'parameter'; currentEditId = 0;
    $('#editModal').data('etype', 'parameter').data('eid', 0);
    var h = '<div class="form-group"><label>参数名</label><input class="form-control" id="edit_name" /></div>';
    h += '<div class="form-group"><label>显示标签</label><input class="form-control" id="edit_label" /></div>';
    h += '<div class="form-group"><label>控件类型</label><select class="form-control" id="edit_controlType">';
    h += '<option value="TextBox">TextBox</option>';
    h += '<option value="ComboBox">ComboBox</option>';
    h += '<option value="DateTimePicker">DateTimePicker</option></select></div>';
    h += '<div class="form-group"><label>数据源JSON</label><textarea class="form-control" id="edit_dataSource" rows="2"></textarea></div>';
    h += '<div class="form-group"><label>默认值</label><input class="form-control" id="edit_defaultValue" /></div>';
    h += '<div class="checkbox"><label><input type="checkbox" id="edit_required" /> 必填</label></div>';
    showEditModal('新增参数', h);
    $('#editModalBody').append('<input type="hidden" id="edit_functionId" value="' + functionId + '" />');
}

function deleteItem(type, id) {
    if (!confirm('确认删除？')) return;
    $.ajax({ url: APP_ROOT + '/api/config/delete', method: 'POST', contentType: 'application/json', data: JSON.stringify({ type: type, id: id }),
        success: function(res) {
            if (res.success) { loadIntentList(); $('#configDetail').html('<p style="margin-top:100px;text-align:center;">已删除</p>'); }
            else alert('删除失败: ' + res.message);
        }
    });
}

function showDataSourceList() {
    highlightTreeItem('');
    $.ajax({
        url: APP_ROOT + '/api/datasource/list',
        success: function(res) {
            if (!res.success) { $('#configDetail').html('<div class="alert alert-danger">加载失败</div>'); return; }
            var data = res.data;
            var html = '<div class="detail-section"><h5>数据源配置</h5></div>';
            if (!data || data.length === 0) {
                html += '<p>暂无数据源，请在 Web.config 的 connectionStrings 中配置</p>';
            } else {
                $.each(data, function(i, ds) {
                    html += '<div class="panel panel-default" style="margin-bottom:10px;"><div class="panel-body" style="padding:10px 15px;">';
                    html += '<strong>' + escHtml(ds.name) + '</strong> <span class="label label-info">' + escHtml(ds.providerType) + '</span>';
                    if (ds.source === 'web.config') html += ' <span class="label label-default">web.config</span>';
                    html += '<div style="margin-top:5px;"><span style="color:#999;font-size:12px;">连接串在 Web.config 中配置</span></div></div></div>';
                });
            }
            $('#configDetail').html(html);
        }
    });
}

function openSqlConverter() {
    var p = '你是一个企业业务系统配置生成器。请根据我的需求生成JSON。';
    p += 'JSON结构：{"name":"意图名称","description":"描述","keywords":"关键词",';
    p += '"functions":[{"name":"功能名称","dataSource":"BusinessDB",';
    p += '"parameters":[{"name":"@参数名","label":"标签",';
    p += '"controlType":"TextBox|ComboBox","dataSource":"[选项1,选项2]","isRequired":false}],';
    p += '"steps":[{"stepType":"sql|show_table|show_text|show_error|end","sqlText":"参数化SQL",';
    p += '"resultVar":"$变量名",';
    p += '"expectOperator":"gt|eq|lt","expectValue":"0","expectOnFail":"goto|show_error|stop",';
    p += '"expectTarget":null,"expectMessage":"诊断信息","displayTitle":"标题","displayConfig":"{}"}]}]}。';
    p += '\n【重要规则】';
    p += '\n1. SQL中的参数必须用 @ 前缀（如 @SampleId），不要用 : 前缀，系统自动转 Oracle 的 :';
    p += '\n2. Oracle 表必须加 schema 前缀，如 FROM schema.table_name，不加报 ORA-00942';
    p += '\n3. 业务库查询必须设置 "dataSource":"BusinessDB"（function级别），步骤级别不加 dataSource';
    p += '\n4. WHERE条件用 (@ItemCode IS NULL OR a.pa1 = @ItemCode) 模式支持参数为空';
    p += '\n5. COUNT必须写 COUNT(*) 而不是 COUNT()';
    p += '\n6. 展示用的 show_table 要设置 displayConfig 如 {"columns":["列1","列2"]}';
    p += '\n7. 第一个SQL通常用 COUNT(*) 判断数据存在，配合 gt + goto/show_error 分支';
    p += '\n8. 最后一步必须是 end，只输出JSON不做解释';
    p += '\n\n我的业务需求：';
    var ta = $('<textarea style="width:100%;height:300px;font-size:12px;font-family:monospace;" readonly></textarea>');
    ta.val(p);
    $('#configDetail').html('<div class="detail-section"><h5>SQL转配置 Prompt</h5><p>复制到ChatGPT/Claude：</p></div>');
    $('#configDetail').append(ta);
    $('#configDetail').append('<p style="margin-top:10px;">然后在配置中点 <strong>导入</strong> 粘贴结果</p>');
}

function rebuildVectors() {
    if (!confirm('确认重建所有意图的向量缓存？')) return;
    $.ajax({ url: APP_ROOT + '/api/config/rebuildVectors', method: 'POST',
        success: function(res) { if (res.success) { alert('重建完成'); } else alert('失败: ' + (res.message || '')); },
        error: function(xhr) { alert('请求失败: ' + xhr.status); }
    });
}

function exportConfig() {
    $.ajax({ url: APP_ROOT + '/api/config/export', method: 'POST',
        success: function(res) {
            if (!res.success || !res.data || !res.data.json) { alert('导出失败'); return; }
            var b = new Blob([res.data.json], { type: 'application/json' });
            var a = document.createElement('a');
            a.href = URL.createObjectURL(b);
            a.download = 'IntentEngine_Config_' + new Date().toISOString().slice(0, 10) + '.json';
            a.click();
        }
    });
}
function importConfig() {
    $('#editModalTitle').text('导入配置');
    $('#editModalBody').html('<p>粘贴AI生成的JSON：</p><textarea class="form-control" id="importJson" rows="10"></textarea>');
    $('#editModal .modal-footer').html('<button type="button" class="btn btn-default" data-dismiss="modal">取消</button>' +
        '<button type="button" class="btn btn-primary" onclick="doImport()">导入</button>');
    $('#editModal').modal('show');
}
function doImport() {
    var j = $('#importJson').val().trim();
    if (!j) { alert('请粘贴JSON'); return; }
    $.ajax({ url: APP_ROOT + '/api/config/import', method: 'POST', contentType: 'application/json', data: JSON.stringify({ json: j }),
        success: function(res) { if (res.success) { alert('导入成功'); $('#editModal').modal('hide'); loadIntentList(); } else alert('导入失败: ' + (res.message || '')); },
        error: function() { alert('网络错误'); }
    });
}

function escHtml(str) { if (!str) return ''; return String(str).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }
function escAttr(str) { if (!str) return ''; return String(str).replace(/"/g, '&quot;').replace(/'/g, '&#39;'); }
