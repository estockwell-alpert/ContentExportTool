using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Install;
using Sitecore.Install.Framework;
using Sitecore.Install.Items;
using Sitecore.Install.Zip;
using Sitecore.Layouts;
using Sitecore.Links;
using Sitecore.Sites;
using System.Runtime.InteropServices;
using Sitecore.Configuration;
using Sitecore.Install.Files;
using Sitecore.Shell.Applications.Install;
using ImageField = Sitecore.Data.Fields.ImageField;

namespace ContentExportTool
{
    public partial class ContentExportAdminPage : Page
    {
        protected void btnGeneratePackage_OnClick(object sender, EventArgs e)
        {
            var contentExportUtil = new ContentExport();
            var packageProject = new PackageProject()
            {
                Metadata =
                    {
                        PackageName = String.IsNullOrEmpty(txtFileName.Value) ? "Content Export Tool " + DateTime.Now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) : txtFileName.Value,
                        Author = "Erica Stockwell-Alpert",
                        Version = txtVersion.Value
                    }
            };

            packageProject.Sources.Clear();
            var source = new ExplicitItemSource();
            source.Name = "Items";

            var _core = Factory.GetDatabase("core");
            var _master = Factory.GetDatabase("master");

            // items
            var coreAppItem = _core.Items.GetItem("/sitecore/content/Applications/Content Export");
            var coreMenuItem =
                _core.Items.GetItem("/sitecore/content/Documents and settings/All users/Start menu/Right/Content Export");

            var template = _master.Items.GetItem("/sitecore/templates/Modules/Content Export Tool");
            var folder = _master.Items.GetItem("/sitecore/system/Modules/Content Export Tool ");

            source.Entries.Add(new ItemReference(coreAppItem.Uri, false).ToString());
            source.Entries.Add(new ItemReference(coreMenuItem.Uri, false).ToString());
            source.Entries.Add(new ItemReference(template.Uri, false).ToString());
            source.Entries.Add(new ItemReference(folder.Uri, false).ToString());

            foreach (var child in template.Axes.GetDescendants())
            {
                source.Entries.Add(new ItemReference(child.Uri, false).ToString());
            }

            packageProject.Sources.Add(source);

            // files
            var fileSource = new ExplicitFileSource();
            fileSource.Name = "Files";

            fileSource.Entries.Add(MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\sitecore\\shell\\Applications\\ContentExport\\ContentExport.aspx"));
            fileSource.Entries.Add(MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\sitecore\\shell\\Applications\\ContentExport\\ContentExport.aspx.cs"));
            fileSource.Entries.Add(MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\sitecore\\shell\\Applications\\ContentExport\\ContentExport.aspx.designer.cs"));
            fileSource.Entries.Add(MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\sitecore\\shell\\Applications\\ContentExport\\jquery-2.2.4.min.js"));
            fileSource.Entries.Add(MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\sitecore\\shell\\Applications\\ContentExport\\jquery-ui.min.js"));
            fileSource.Entries.Add(MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\sitecore\\shell\\Applications\\ContentExport\\ContentExportScripts.js"));
            fileSource.Entries.Add(
                MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\temp\\IconCache\\Network\\16x16\\download.png"));
            fileSource.Entries.Add(
                MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\temp\\IconCache\\Network\\32x32\\download.png"));
            fileSource.Entries.Add(
                MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\temp\\IconCache\\Network\\24x24\\download.png"));
            fileSource.Entries.Add(MainUtil.MapPath("C:\\inetpub\\wwwroot\\canada10.erica.velir.com\\sitecore\\shell\\Themes\\Standard\\Images\\ProgressIndicator\\sc-spinner32.gif"));

            packageProject.Sources.Add(fileSource);

            packageProject.SaveProject = true;

            var fileName = packageProject.Metadata.PackageName + ".zip";
            var filePath = contentExportUtil.FullPackageProjectPath(fileName);

            using (var writer = new PackageWriter(filePath))
            {
                Sitecore.Context.SetActiveSite("shell");
                writer.Initialize(Installer.CreateInstallationContext());
                PackageGenerator.GeneratePackage(packageProject, writer);
                Sitecore.Context.SetActiveSite("website");

                Response.Clear();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", string.Format("attachment;filename={0}", fileName));
                Response.ContentType = "application/zip";

                byte[] data = new WebClient().DownloadData(filePath);
                Response.BinaryWrite(data);
                Response.Flush();
                Response.End();
            }
        }
    }
}
