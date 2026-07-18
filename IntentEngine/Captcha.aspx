<%@ Page Language="C#" %>
<%@ Import Namespace="System.Drawing" %>
<%@ Import Namespace="System.Drawing.Imaging" %>
<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        Response.Clear();
        Response.ContentType = "image/png";

        var rnd = new Random();

        string code = Session["Captcha"] as string;
        if (string.IsNullOrEmpty(code))
        {
            string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            code = "";
            for (int i = 0; i < 4; i++)
                code += chars[rnd.Next(chars.Length)];
            Session["Captcha"] = code;
        }

        using (var bmp = new Bitmap(100, 40))
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            for (int i = 0; i < 3; i++)
            {
                using (var pen = new Pen(Color.FromArgb(rnd.Next(200), rnd.Next(200), rnd.Next(200)), 1))
                    g.DrawLine(pen, rnd.Next(100), rnd.Next(40), rnd.Next(100), rnd.Next(40));
            }
            using (var font = new Font("Arial", 20, FontStyle.Bold | FontStyle.Italic))
            {
                for (int i = 0; i < code.Length; i++)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(rnd.Next(60), rnd.Next(60), rnd.Next(60))))
                        g.DrawString(code[i].ToString(), font, brush, 2 + i * 24, rnd.Next(6));
                }
            }
            for (int i = 0; i < 30; i++)
                bmp.SetPixel(rnd.Next(100), rnd.Next(40), Color.FromArgb(rnd.Next(200), rnd.Next(200), rnd.Next(200)));

            bmp.Save(Response.OutputStream, ImageFormat.Png);
        }
    }
</script>
