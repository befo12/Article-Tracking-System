using System;
using System.Data;
using System.Linq;
using System.Web.UI;

namespace WebApplication1.Pages
{
    public partial class Article : Page
    {
        private const string SLib = "library_table";
        private int Aid
        {
            get
            {
                int id;
                return int.TryParse(Request.QueryString["aid"], out id) ? id : 0;
            }
        }

        private DataRow FindRow()
        {
            var t = Session[SLib] as DataTable;
            if (t == null) return null;
            return t.AsEnumerable().FirstOrDefault(r => r.Field<int>("Id") == Aid);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                LoadData();
        }

        private void LoadData()
        {
            var row = FindRow();
            if (row == null)
            {
                // kayıt yoksa kütüphaneye dön
                Response.Redirect("~/Pages/Library.aspx");
                return;
            }

            lblTitle.Text = row.Field<string>("Title");
            lblCat.Text = row.Field<string>("Category");
            chkRead.Checked = row.Field<bool>("IsRead");
            txtNote.Text = row.Field<string>("Note");

            // "Tartış" linki (Discuss.aspx?aid=...)
            lnkDiscuss.NavigateUrl = "~/Pages/Discuss.aspx?aid=" + Aid;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            var t = Session[SLib] as DataTable;
            var row = FindRow();
            if (row == null) return;

            row["IsRead"] = chkRead.Checked;
            row["Note"] = txtNote.Text.Trim();

            Session[SLib] = t;
            lblInfo.Text = "Kaydedildi.";
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            var t = Session[SLib] as DataTable;
            var row = FindRow();
            if (t != null && row != null)
            {
                t.Rows.Remove(row);
                Session[SLib] = t;
            }
            Response.Redirect("~/Pages/Library.aspx");
        }
    }
}
