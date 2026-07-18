// Intent Engine - 主应用
var currentFunctionId = 0;
var currentIntentId = 0;

$(function() {
    checkAuth();
});

function checkAuth() {
    $.ajax({
        url: APP_ROOT + '/api/auth/status',
        method: 'GET',
        success: function(res) {
            if (res.success) {
                $('#loginOverlay').hide();
                $('#mainApp').show();
                initSystem();
            } else {
                $('#loginOverlay').show();
                $('#mainApp').hide();
            }
        },
        error: function() {
            $('#loginOverlay').show();
            $('#mainApp').hide();
        }
    });
}

function initSystem() {
    $('#statusLabel').html('<span class="label label-warning">初始化中...</span>');
    $.ajax({
        url: APP_ROOT + '/api/system/status',
        success: function(res) {
            if (res.success && res.data) {
                var s = res.data;
                var modelText = s.embedding ? (s.embedding.model || '') + (s.embedding.ready ? ' ✅' : ' ⚠️') : '未知';
                $('#modelStatus').text('模型: ' + modelText);
            }
            $('#statusLabel').html('<span class="label label-success">就绪</span>');
        },
        error: function() {
            $('#statusLabel').html('<span class="label label-success">就绪</span>');
        }
    });

    $('#searchInput').on('keydown', function(e) {
        if (e.keyCode === 13) doSearch();
    });
}

function doLogout() {
    $.ajax({
        url: APP_ROOT + '/api/auth/logout',
        method: 'POST',
        success: function() {
            window.location.href = 'Default.aspx';
        }
    });
}

function clearAll() {
    $('#searchInput').val('');
    $('#candidateSection').hide();
    $('#functionSection').hide();
    $('#resultArea').html(
        '<div class="jumbotron" style="text-align:center;color:#ccc;background:transparent;">' +
        '<h3>欢迎使用 Intent Engine</h3><p>输入业务描述，系统自动匹配功能</p></div>'
    );
    $('#queryTime').text('');
    currentIntentId = 0;
    currentFunctionId = 0;
}

$(document).keydown(function(e) {
    if (e.keyCode === 27) { // ESC
        $('.modal').modal('hide');
    }
});
