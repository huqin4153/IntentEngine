<%@ Page Language="C#" AutoEventWireup="true" ResponseEncoding="utf-8" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Charset = "utf-8";
        Response.ContentEncoding = System.Text.Encoding.UTF8;
        if (!IsPostBack && Session["IsAuthenticated"] == null)
            GenerateCaptcha();
    }

    protected string CaptchaBase64 { get; set; }

    private void GenerateCaptcha()
    {
        var rnd = new Random();
        string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        string code = "";
        for (int i = 0; i < 4; i++)
            code += chars[rnd.Next(chars.Length)];
        Session["Captcha"] = code;

        using (var bmp = new Bitmap(100, 40))
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            for (int i = 0; i < 3; i++)
                using (var pen = new Pen(Color.FromArgb(rnd.Next(200), rnd.Next(200), rnd.Next(200)), 1))
                    g.DrawLine(pen, rnd.Next(100), rnd.Next(40), rnd.Next(100), rnd.Next(40));
            using (var font = new Font("Arial", 20, FontStyle.Bold | FontStyle.Italic))
                for (int i = 0; i < code.Length; i++)
                    using (var brush = new SolidBrush(Color.FromArgb(rnd.Next(60), rnd.Next(60), rnd.Next(60))))
                        g.DrawString(code[i].ToString(), font, brush, 2 + i * 24, rnd.Next(6));
            for (int i = 0; i < 30; i++)
                bmp.SetPixel(rnd.Next(100), rnd.Next(40), Color.FromArgb(rnd.Next(200), rnd.Next(200), rnd.Next(200)));
            using (var ms = new System.IO.MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                CaptchaBase64 = Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    protected string GetLoginErr()
    {
        if (Request.QueryString["captcha"] == "1") return "验证码错误或已过期";
        if (Request.QueryString["error"] == "1") return "用户名或密码错误";
        return null;
    }
</script>
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1.0" />
<title>Intent Engine - 智能业务查询系统</title>
<link href="Static/lib/bootstrap.min.css" rel="stylesheet" />
<link href="Static/css/app.css" rel="stylesheet" />
</head>
<body>

<div id="loginOverlay" style="position:fixed;top:0;left:0;width:100%;height:100%;background:#f0f2f5;z-index:9999;display:flex;align-items:center;justify-content:center;">
    <form method="post" action="Login.aspx" style="background:#fff;border-radius:8px;padding:40px 30px;width:340px;box-shadow:0 5px 30px rgba(0,0,0,0.15);text-align:center;">
        <h3 style="margin-bottom:5px;">Intent Engine</h3>
        <p style="color:#999;margin-bottom:30px;">智能业务查询系统</p>
        <div class="form-group">
            <input type="text" class="form-control" name="username" placeholder="用户名" style="font-size:16px;height:44px;" required />
        </div>
        <div class="form-group">
            <input type="password" class="form-control" name="password" placeholder="密码" style="font-size:16px;height:44px;" required />
        </div>
        <div class="form-group" style="text-align:left;">
            <div style="display:flex;align-items:center;gap:8px;">
                <img src="data:image/png;base64,<%= CaptchaBase64 %>" id="captchaImg" onclick="window.location.href=window.location.pathname+'?r='+Date.now()" style="cursor:pointer;border:1px solid #ddd;border-radius:4px;" title="点击刷新" />
                <input type="text" name="captcha" maxlength="4" placeholder="验证码" style="font-size:16px;height:44px;" class="form-control" />
            </div>
            <div style="font-size:12px;color:#999;margin-top:3px;">点击图片刷新</div>
        </div>
        <% var loginErr = GetLoginErr(); if (loginErr != null) { %>
        <div class="alert alert-danger" style="padding:8px;font-size:13px;"><%= loginErr %></div>
        <% } %>
        <button type="submit" class="btn btn-primary btn-block" style="font-size:16px;padding:10px;">登 录</button>
    </form>
</div>

<div id="mainApp" style="display:none;">
    <nav class="navbar navbar-inverse navbar-fixed-top">
        <div class="container-fluid">
            <div class="navbar-header">
                <a class="navbar-brand" href="#"><span class="glyphicon glyphicon-search"></span> Intent Engine</a>
            </div>
            <div class="navbar-right" style="margin-right:15px;">
                <span class="navbar-text" id="statusLabel"><span class="label label-success">就绪</span></span>
                <button class="btn btn-default navbar-btn" onclick="openConfig()"><span class="glyphicon glyphicon-cog"></span> 配置管理</button>
                <button class="btn btn-default navbar-btn" onclick="doLogout()"><span class="glyphicon glyphicon-log-out"></span> 退出</button>
            </div>
        </div>
    </nav>
    <div class="container-fluid" style="margin-top:70px;">
        <div class="row">
            <div class="col-md-8 col-md-offset-2">
                <div class="input-group input-group-lg">
                    <input type="text" class="form-control" id="searchInput" placeholder="请输入业务描述，如：查看样品信息、查询订单..." autofocus />
                    <span class="input-group-btn">
                        <button class="btn btn-primary" onclick="doSearch()"><span class="glyphicon glyphicon-search"></span> 搜索</button>
                        <button class="btn btn-default" onclick="clearAll()"><span class="glyphicon glyphicon-erase"></span> 清除</button>
                    </span>
                </div>
                <p class="help-block" style="text-align:center;">输入 <code>#帮助</code> 查看所有意图 | <code>#退出</code> 退出系统</p>
            </div>
        </div>
        <div class="row" id="candidateSection" style="display:none;">
            <div class="col-md-8 col-md-offset-2"><div class="panel panel-default"><div class="panel-heading">候选意图</div><div class="list-group" id="candidateList"></div></div></div>
        </div>
        <div class="row" id="functionSection" style="display:none;">
            <div class="col-md-8 col-md-offset-2"><ul class="nav nav-tabs" id="functionTabs" role="tablist"></ul></div>
        </div>
        <div class="row" style="margin-top:15px;">
            <div class="col-md-12"><div id="resultArea"><div class="jumbotron" style="text-align:center;color:#ccc;background:transparent;"><h3>欢迎使用 Intent Engine</h3><p>输入业务描述，系统自动匹配功能</p></div></div></div>
        </div>
    </div>
    <nav class="navbar navbar-default navbar-fixed-bottom">
        <div class="container-fluid">
            <p class="navbar-text" style="color:#666;">
                <span class="glyphicon glyphicon-ok-circle text-success"></span> 就绪
                <span style="margin:0 10px;">|</span><span id="modelStatus">模型: 加载中...</span>
                <span style="margin:0 10px;">|</span><span id="queryTime"></span>
            </p>
        </div>
    </nav>
</div>

<div class="modal fade" id="paramModal"><div class="modal-dialog"><div class="modal-content">
    <div class="modal-header"><button type="button" class="close" data-dismiss="modal">&times;</button><h4 class="modal-title">查询参数</h4></div>
    <div class="modal-body" id="paramBody"></div>
    <div class="modal-footer">
        <button type="button" class="btn btn-default" data-dismiss="modal">取消</button>
        <button type="button" class="btn btn-primary" onclick="submitParams()"><span class="glyphicon glyphicon-ok"></span> 确认</button>
    </div>
</div></div></div>

<div class="modal fade" id="configModal"><div class="modal-dialog modal-lg" style="width:90%;"><div class="modal-content">
    <div class="modal-header">
        <button type="button" class="close" data-dismiss="modal">&times;</button>
        <h4 class="modal-title"><span class="glyphicon glyphicon-cog"></span> 配置管理
        <button class="btn btn-xs btn-success pull-right" onclick="exportConfig()" style="margin-left:5px;"><span class="glyphicon glyphicon-export"></span> 导出</button>
        <button class="btn btn-xs btn-info pull-right" onclick="importConfig()" style="margin-left:5px;"><span class="glyphicon glyphicon-import"></span> 导入</button>
        <button class="btn btn-xs btn-primary pull-right" onclick="showDataSourceList()" style="margin-left:5px;"><span class="glyphicon glyphicon-flash"></span> 数据源</button>
        <button class="btn btn-xs btn-default pull-right" onclick="openSqlConverter()" style="margin-left:5px;"><span class="glyphicon glyphicon-console"></span> SQL转配置</button>
        <button class="btn btn-xs btn-warning pull-right" onclick="rebuildVectors()" style="margin-left:5px;"><span class="glyphicon glyphicon-refresh"></span> 重建向量</button></h4>
    </div>
    <div class="modal-body" style="min-height:400px;max-height:70vh;overflow:auto;display:flex;">
        <div style="width:260px;border-right:1px solid #ddd;padding-right:10px;overflow:auto;">
            <div class="input-group input-group-sm" style="margin-bottom:10px;">
                <input type="text" class="form-control" id="configSearch" placeholder="搜索意图..." />
                <span class="input-group-btn"><button class="btn btn-default" onclick="addIntent()">+ 新增</button></span>
            </div>
            <div id="configTree"></div>
        </div>
        <div style="flex:1;padding-left:15px;overflow:auto;" id="configDetail">
            <p class="text-muted" style="margin-top:80px;text-align:center;">请从左侧选择配置项</p>
        </div>
    </div>
</div></div></div>

<div class="modal fade" id="editModal"><div class="modal-dialog"><div class="modal-content">
    <div class="modal-header"><button type="button" class="close" data-dismiss="modal">&times;</button><h4 class="modal-title" id="editModalTitle">编辑</h4></div>
    <div class="modal-body" id="editModalBody"></div>
    <div class="modal-footer">
        <button type="button" class="btn btn-default" data-dismiss="modal">取消</button>
        <button type="button" class="btn btn-primary" onclick="saveEdit()"><span class="glyphicon glyphicon-floppy-disk"></span> 保存</button>
    </div>
</div></div></div>

<script src="Static/lib/jquery-3.5.1.min.js"></script>
<script src="Static/lib/bootstrap.min.js"></script>
<script>var APP_ROOT = '<%= ResolveUrl("~") %>'; if (APP_ROOT.endsWith('/')) APP_ROOT = APP_ROOT.slice(0, -1);</script>
<script src="Static/js/app.js?v=20260712"></script>
<script src="Static/js/intent.js?v=20260712"></script>
<script src="Static/js/flow.js?v=20260712"></script>
<script src="Static/js/config.js?v=20260712"></script>
</body>
</html>
