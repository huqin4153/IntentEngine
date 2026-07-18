// Intent Engine - 意图搜索与匹配

var searchTimer;
var lastSearchText = '';

// 搜索（防抖）
function doSearch() {
    var text = $('#searchInput').val().trim();
    if (!text) { clearAll(); return; }

    // 特殊指令的前端处理
    if (text.startsWith('#')) {
        var cmd = text.toLowerCase().trim();
        if (cmd === '#help' || cmd === '#帮助' || cmd === '#?') {
            showHelp();
            return;
        }
        if (cmd === '#exit' || cmd === '#退出' || cmd === '#quit') {
            if (confirm('确认退出系统？')) {
                doLogout();
            }
            return;
        }
        if (cmd === '#clear' || cmd === '#清除') {
            clearAll();
            return;
        }
    }

    // 防抖搜索
    clearTimeout(searchTimer);
    searchTimer = setTimeout(function() {
        if (text === lastSearchText) return;
        lastSearchText = text;

        $.ajax({
            url: APP_ROOT + '/api/intent/match',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ text: text }),
            success: function(res) {
                $('#queryTime').text('查询: ' + res.elapsedMs + 'ms');
                if (res.success && res.data && res.data.results && res.data.results.length > 0) {
                    renderCandidates(res.data.results);
                } else {
                    $('#candidateSection').show();
                    $('#candidateList').html(
                        '<div class="list-group-item" style="cursor:default;color:#999;">' +
                        '未找到匹配的意图，请换一种描述试试</div>'
                    );
                    $('#functionSection').hide();
                }
            },
            error: function() {
                $('#candidateSection').show();
                $('#candidateList').html(
                    '<div class="list-group-item list-group-item-danger">搜索失败，请稍后重试</div>'
                );
            }
        });
    }, 300);
}

// 响应输入框实时搜索
$(document).on('input', '#searchInput', function() {
    var text = $(this).val().trim();
    if (text.length === 0) {
        $('#candidateSection').hide();
        $('#functionSection').hide();
        return;
    }
    doSearch();
});

// 渲染候选意图
function renderCandidates(results) {
    $('#candidateSection').show();
    $('#functionSection').hide();

    var html = '';
    $.each(results, function(i, r) {
        var badgeClass = 'badge-confidence-' + (r.confidence === '高' ? 'high' : r.confidence === '中' ? 'medium' : 'low');
        var similarityClass = r.similarity >= 65 ? 'text-success' : (r.similarity >= 40 ? 'text-warning' : 'text-muted');
        html += '<a class="list-group-item" onclick="selectIntent(' + r.intent.id + ')">' +
                '<h4 class="list-group-item-heading" style="margin-bottom:3px;">' +
                r.intent.name +
                ' <small class="' + similarityClass + '">' + r.similarity + '%</small>' +
                (r.isFallback ? ' <span class="label label-default">关键词匹配</span>' : '') +
                '</h4>' +
                '<p class="list-group-item-text" style="color:#888;">' +
                '<span class="label label-info">' + (r.intent.category || '未分类') + '</span> ' +
                (r.intent.description || '') +
                '</p>' +
                '</a>';
    });
    $('#candidateList').html(html);
}

// 选择意图
function selectIntent(intentId) {
    currentIntentId = intentId;

    // 高亮选中的候选
    $('#candidateList .list-group-item').removeClass('active');
    $('#candidateList .list-group-item').eq(
        $('#candidateList .list-group-item').index(
            $('[onclick*="' + intentId + '"]')
        )
    ).addClass('active');

    // 加载功能列表
    $.ajax({
        url: APP_ROOT + '/api/flow/functions',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ intentId: intentId }),
        success: function(res) {
            if (res.success && res.data && res.data.length > 0) {
                renderFunctionTabs(res.data);
                // 自动选择第一个功能
                if (res.data.length > 0) {
                    selectFunction(res.data[0].id);
                }
            }
        }
    });
}

// 渲染功能标签
function renderFunctionTabs(functions) {
    $('#functionSection').show();
    var html = '';
    $.each(functions, function(i, f) {
        html += '<li role="presentation" class="' + (i === 0 ? 'active' : '') + '">' +
                '<a onclick="selectFunction(' + f.id + ')" role="tab">' +
                f.name +
                '</a></li>';
    });
    $('#functionTabs').html(html);
}

// 帮助弹窗
function showHelp() {
    $.ajax({
        url: APP_ROOT + '/api/intent/list',
        success: function(res) {
            if (res.success && res.data) {
                var html = '<div class="list-group">';
                $.each(res.data, function(i, intent) {
                    html += '<div class="list-group-item">' +
                            '<h5>' + intent.name + '</h5>' +
                            '<p style="color:#888;margin:0;">' +
                            '<span class="label label-info">' + (intent.category || '未分类') + '</span> ' +
                            (intent.description || '') +
                            '</p></div>';
                });
                html += '</div>';

                $('#paramBody').html(html);
                $('#paramModal .modal-title').text('📋 可用意图列表');
                $('#paramModal .modal-footer').html(
                    '<button type="button" class="btn btn-default" data-dismiss="modal">关闭</button>'
                );
                $('#paramModal').modal('show');
            }
        }
    });
}
