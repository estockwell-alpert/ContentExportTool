using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI.WebControls;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Mvc.Extensions;
using Sitecore.Services.Core.ComponentModel;
using ImageField = Sitecore.Data.Fields.ImageField;

namespace ContentExportTool
{
    public partial class ContentExport : Sitecore.sitecore.admin.AdminPage
    {
        private Database _db;
        private string _settingsFilePath = AppDomain.CurrentDomain.BaseDirectory + @"\sitecore\admin\ContentExportSettings.txt";
        private bool _sitecoreItemApiEnabled;

        protected void Page_Load(object sender, EventArgs e)
        {
            CheckSitecoreItemApiEnabled();
            PhApiMessage.Visible = !_sitecoreItemApiEnabled;
            PhApiMessageBrowse.Visible = !_sitecoreItemApiEnabled;
            PhApiMessageTempaltes.Visible = !_sitecoreItemApiEnabled;
            litSavedMessage.Text = String.Empty;
            phOverwriteScript.Visible = false;
            litFastQueryTest.Text = String.Empty;
            if (!IsPostBack)
            {
                txtSaveSettingsName.Value = string.Empty;
                PhBrowseTree.Visible = false;
                PhBrowseTemplates.Visible = false;
                PhBrowseFields.Visible = false;
                var databaseNames = Sitecore.Configuration.Factory.GetDatabaseNames().ToList();
                // make web the default database
                var webDb = databaseNames.FirstOrDefault(x => x.ToLower().Contains("web"));
                if (webDb != null)
                {
                    databaseNames.Remove(webDb);
                    databaseNames.Insert(0, webDb);
                }
                ddDatabase.DataSource = databaseNames;
                ddDatabase.DataBind();

                var languages = GetSiteLanguages().Select(x => x.GetDisplayName()).OrderBy(x => x).ToList();
                languages.Insert(0, "");
                ddLanguages.DataSource = languages;
                ddLanguages.DataBind(); 

                SetSavedSettingsDropdown();
            }
        }

        protected List<Language> GetSiteLanguages()
        {
            var database = ddDatabase.SelectedValue;
            SetDatabase(database);
            var installedLanguages = LanguageManager.GetLanguages(_db);

            return installedLanguages.ToList();
        }

        protected void SetSavedSettingsDropdown()
        {
            var savedSettings = ReadSettingsFromFile();
            if (savedSettings != null)
            {
                var settingsNames = savedSettings.Settings.Select(x => x.Name).ToList();
                settingsNames.Insert(0, "");
                ddSavedSettings.DataSource = settingsNames;
                ddSavedSettings.DataBind();
            }
        }

        protected string GetSitecoreTreeHtml()
        {
            var database = ddDatabase.SelectedValue;
            SetDatabase(database);
            var sitecoreTreeHtml = "<ul>";
            var contentRoot = _db.GetItem("/sitecore/content");
            var mediaRoot = _db.GetItem("/sitecore/media library");

            sitecoreTreeHtml += GetItemAndChildren(contentRoot);

            sitecoreTreeHtml += GetItemAndChildren(mediaRoot);

            sitecoreTreeHtml += "</ul>";

            return sitecoreTreeHtml;
        }

        protected string GetAvailableTemplates()
        {
            var database = ddDatabase.SelectedValue;
            SetDatabase(database);
            var startItem = _db.GetItem("/sitecore/templates");

            var html = "<ul>";

            html += GetTemplateTree(startItem);

            html += "</ul>";
            return html;
        }

        protected string GetAvailableFields()
        {
            var database = ddDatabase.SelectedValue;
            SetDatabase(database);

            string html = "<ul>";

            var templateList = new List<TemplateItem>();
            var startItem = _db.GetItem("/sitecore/templates");
            if (!String.IsNullOrEmpty(inputTemplates.Value))
            {
                var templateNames = inputTemplates.Value.Split(',');
                foreach (var templateName in templateNames.Where(x => !String.IsNullOrEmpty(x)))
                {
                    var template =
                        startItem.Axes.GetDescendants().Where(x => x.TemplateName == "Template").FirstOrDefault(x => x.Name.ToLower() == templateName.Trim().ToLower());
                    if (template != null)
                    {
                        TemplateItem templateItem = _db.GetTemplate(template.ID);
                        if (templateItem != null)
                        {
                            templateList.Add(templateItem);
                        }
                    }
                }
            }
            else
            {
                var templateItems = startItem.Axes.GetDescendants().Where(x => x.TemplateName == "Template");
                templateList.AddRange(templateItems.Select(item => _db.GetTemplate(item.ID)));
                templateList = templateList.OrderBy(x => x.Name).ToList();
            }

            foreach (var template in templateList)
            {
                var fields = template.Fields.Where(x => x.Name[0] != '_');
                fields = fields.OrderBy(x => x.Name);
                if (fields.Any())
                {
                    html += "<li data-name='" + template.Name.ToLower() + "' class='template-heading'>";
                    html += string.Format(
                        "<a class='browse-expand' onclick='expandNode($(this))'>+</a><span>{0}</span><a class='select-all' href='javascript:void(0)' onclick='selectAllFields($(this))'>select all</a>",
                        template.Name);
                    html += "<ul class='field-list'>";
                    foreach (var field in fields)
                    {
                        html +=
                            string.Format(
                                "<li data-name='{2}'><a class='field-node' href='javascript:void(0)' onclick='selectBrowseNode($(this));' data-id='{0}' data-name='{1}'>{1}</a></li>",
                                field.ID, field.Name, field.Name.ToLower());
                    }
                    html += "</ul>";
                    html += "</li>";
                }
            }

            html += "</ul>";

            return html;
        }

        protected string GetItemAndChildren(Item item)
        {
            var children = item.GetChildren().Cast<Item>();

            var nodeHtml = "<li data-name='" + item.Name.ToLower() + "' data-id='" + item.ID + "'>";

            if (children.Any())
            {
                nodeHtml += "<a class='browse-expand' onclick='expandNode($(this))'>+</a>";
            }

            nodeHtml += string.Format("<a class='sitecore-node' href='javascript:void(0)' onclick='selectNode($(this));' data-path='{0}'>{1}</a>", item.Paths.Path, item.Name);

            if (!_sitecoreItemApiEnabled)
            {
                // turn on notification message
                if (children.Any())
                {
                    nodeHtml += "<ul>";
                    foreach (Item child in children)
                    {
                        nodeHtml += GetItemAndChildren(child);
                    }
                    nodeHtml += "</ul>";
                }
            }

            nodeHtml += "</li>";
            return nodeHtml;
        }

        protected string GetTemplateTree(Item item)
        {
            var children = item.GetChildren();

            var nodeHtml = "<li data-name='" + item.Name.ToLower() + "' data-id='" + item.ID + "'>";

            if (item.TemplateName == "Template")
            {
                nodeHtml +=
                    string.Format(
                        "<a data-id='{0}' data-name='{1}' class='template-link' href='javascript:void(0)' onclick='selectBrowseNode($(this));'>{1}</a>",
                        item.ID, item.Name);
            }
            else
            {
                if (children.Any())
                {
                    nodeHtml += "<a class='browse-expand' onclick='expandNode($(this))'>+</a><span></span>";
                }

                nodeHtml += string.Format("<span>{0}</span>", item.Name);

                if (!_sitecoreItemApiEnabled)
                {
                    // turn on notification message
                    if (children.Any())
                    {
                        nodeHtml += "<ul>";
                        foreach (Item child in children)
                        {
                            nodeHtml += GetTemplateTree(child);
                        }
                        nodeHtml += "</ul>";
                    }
                }

            }

            nodeHtml += "</li>";
            return nodeHtml;
        }

        protected override void OnInit(EventArgs e)
        {
            base.CheckSecurity(true); //Required!
            base.OnInit(e);
        }

        protected bool SetDatabase()
        {
            var databaseName = ddDatabase.SelectedValue;
            if (chkWorkflowName.Checked || chkWorkflowState.Checked)
            {
                databaseName = "master";
            }

            if (String.IsNullOrEmpty(databaseName))
            {
                databaseName = "master";
            }

            _db = Sitecore.Configuration.Factory.GetDatabase(databaseName);
            return true;
        }

        protected void SetDatabase(string databaseName)
        {
            _db = Sitecore.Configuration.Factory.GetDatabase(databaseName);
        }

        protected void btnRunExport_OnClick(object sender, EventArgs e)
        {
            litFastQueryTest.Text = "";

            try
            {
                var fieldString = inputFields.Value;

                var includeWorkflowState = chkWorkflowState.Checked;
                var includeworkflowName = chkWorkflowName.Checked;

                if (!SetDatabase())
                {
                    litFeedback.Text = "You must enter a custom database name, or select a database from the dropdown";
                    return;
                }


                if (_db == null)
                {
                    litFeedback.Text = "Invalid database. Selected database does not exist.";
                    return;
                }                

                var includeIds = chkIncludeIds.Checked;
                var includeLinkedIds = chkIncludeLinkedIds.Checked;
                var includeName = chkIncludeName.Checked;
                var includeRawHtml = chkIncludeRawHtml.Checked;
                var includeTemplate = chkIncludeTemplate.Checked;

                var includeDateCreated = chkDateCreated.Checked;
                var includeCreatedBy = chkCreatedBy.Checked;
                var includeDateModified = chkDateModified.Checked;
                var includeModifiedBy = chkModifiedBy.Checked;
                var neverPublish = chkNeverPublish.Checked;
                var includeReferrers = chkReferrers.Checked;

                var allLanguages = chkAllLanguages.Checked;
                var selectedLanguage = ddLanguages.SelectedValue;

                var templateString = inputTemplates.Value;
                var templates = templateString.ToLower().Split(',').Select(x => x.Trim()).ToList();

                if (chkIncludeInheritance.Checked)
                {
                    var inheritors = new List<string>();
                    var templateRoot = _db.GetItem("/sitecore/templates");
                    var templateItems = templateRoot.Axes.GetDescendants().Where(x => x.TemplateName == "Template");
                    var templateItems1 = templateItems as Item[] ?? templateItems.ToArray();
                    var enumerable = templateItems as Item[] ?? templateItems1.ToArray();
                    foreach (var template in templates)
                    {
                        // get all template items that include template in base templates
                        
                        var templateItem =
                            enumerable.FirstOrDefault(
                                x =>
                                    x.Name.ToLower() == template.ToLower() ||
                                    x.ID.ToString().ToLower().Replace("{", string.Empty).Replace("}", string.Empty) ==
                                    template.Replace("{", string.Empty).Replace("}", string.Empty));

                        if (templateItem != null)
                        {
                            foreach (var item in templateItems1)
                            {
                                var baseTemplatesField = item.Fields["__Base template"];
                                if (baseTemplatesField != null)
                                {
                                    if (FieldTypeManager.GetField(baseTemplatesField) is MultilistField)
                                    {
                                        MultilistField field = FieldTypeManager.GetField(baseTemplatesField) as MultilistField;
                                        var inheritedTemplates = field.TargetIDs.ToList();
                                        if (inheritedTemplates.Any(x => x == templateItem.ID))
                                        {
                                            inheritors.Add(item.ID.ToString().ToLower());
                                        }
                                    }
                                }
                            }

                        }
                    }
                    templates.AddRange(inheritors);
                }
                                  
                List<Item> items = GetItems();

                var fields = fieldString.Split(',').Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x)).ToList();

                if (chkAllFields.Checked)
                {
                    fields = new List<string>();
                }

                Response.Clear();
                Response.Buffer = true;
                var fileName = !String.IsNullOrEmpty(txtFileName.Value) ? txtFileName.Value : "ContentExport";
                Response.AddHeader("content-disposition", String.Format("attachment;filename={0}.xls", fileName));
                Response.Charset = "";
                Response.ContentType = "application/vnd.ms-excel";

                using (StringWriter sw = new StringWriter())
                {
                    var headingString = "Item Path\t"
                                        + (includeName ? "Name\t" : string.Empty)
                                        + (includeIds ? "Item ID\t" : string.Empty)
                                        + (includeTemplate ? "Template\t" : string.Empty)
                                        +
                                        (allLanguages || !String.IsNullOrEmpty(selectedLanguage)
                                            ? "Language\t"
                                            : string.Empty)
                                        + (includeDateCreated ? "Created\t" : string.Empty)
                                        + (includeCreatedBy ? "Created By\t" : string.Empty)
                                        + (includeDateModified ? "Modified\t" : string.Empty)
                                        + (includeModifiedBy ? "Modified By\t" : string.Empty)
                                        + (neverPublish ? "Never Publish\t" : string.Empty)
                                        + (includeworkflowName ? "Workflow\t" : string.Empty)
                                        + (includeWorkflowState ? "Workflow State\t" : string.Empty)
                                        + (includeReferrers ? "Referrers\t" : string.Empty);

                    var dataLines = new List<string>();

                    foreach (var baseItem in items)
                    {
                        var itemVersions = new List<Item>();
                        if (allLanguages)
                        {
                            foreach (var language in baseItem.Languages)
                            {
                                var languageItem = baseItem.Database.GetItem(baseItem.ID, language);
                                if (languageItem.Versions.Count > 0)
                                {
                                    itemVersions.Add(languageItem);
                                }
                            }
                        }else if (!String.IsNullOrEmpty(selectedLanguage))
                        {
                            foreach (var language in baseItem.Languages)
                            {
                                if (language.GetDisplayName() == selectedLanguage)
                                {
                                    var languageItem = baseItem.Database.GetItem(baseItem.ID, language);
                                    if (languageItem.Versions.Count > 0)
                                    {
                                        itemVersions.Add(languageItem);
                                    }
                                }
                            }
                        }
                        else
                        {
                            itemVersions.Add(baseItem);
                        }

                        foreach (var item in itemVersions)
                        {
                            var itemPath = item.Paths.ContentPath;
                            var itemLine = itemPath + "\t";

                            if (includeName)
                            {
                                itemLine += item.Name + "\t";
                            }                     

                            if (includeIds)
                            {
                                itemLine += item.ID + "\t";
                            }

                            if (includeTemplate)
                            {
                                var template = item.TemplateName;
                                itemLine += template + "\t";
                            }

                            if (allLanguages || !String.IsNullOrEmpty(selectedLanguage))
                            {
                                itemLine += item.Language.GetDisplayName() + "\t";
                            }

                            if (includeDateCreated)
                            {
                                itemLine += item.Statistics.Created.ToString("d") + "\t";
                            }
                            if (includeCreatedBy)
                            {
                                itemLine += item.Statistics.CreatedBy + "\t";
                            }
                            if (includeDateModified)
                            {
                                itemLine += item.Statistics.Updated.ToString("d") + "\t";
                            }
                            if (includeModifiedBy)
                            {
                                itemLine += item.Statistics.UpdatedBy + "\t";
                            }
                            if (neverPublish)
                            {
                                var neverPublishVal = item.Publishing.NeverPublish;
                                itemLine += neverPublishVal.ToString() + "\t";
                            }

                            if (chkAllFields.Checked)
                            {
                                item.Fields.ReadAll();
                                foreach (Field field in item.Fields)
                                {
                                    if (field.Name.StartsWith("__")) continue;
                                    if (fields.All(x => x != field.Name))
                                    {
                                        fields.Add(field.Name);
                                    }
                                }
                            }

                            if (includeWorkflowState || includeworkflowName)
                            {
                                var workflowProvider = item.Database.WorkflowProvider;
                                if (workflowProvider == null)
                                {
                                    if (includeworkflowName && includeWorkflowState)
                                    {
                                        itemLine += "\t";
                                    }
                                    itemLine += "\t";
                                }
                                else
                                {
                                    var workflow = workflowProvider.GetWorkflow(item);
                                    if (workflow == null)
                                    {
                                        if (includeworkflowName && includeWorkflowState)
                                        {
                                            itemLine += "\t";
                                        }
                                        else
                                        {
                                            itemLine += "\t";
                                        }
                                    }
                                    else
                                    {
                                        if (includeworkflowName)
                                        {
                                            itemLine += workflow + "\t";
                                        }
                                        if (includeWorkflowState)
                                        {
                                            var workflowState = workflow.GetState(item);
                                            itemLine += workflowState.DisplayName + "\t";
                                        }
                                    }
                                }
                            }

                            if (includeReferrers)
                            {
                                var referrers = Globals.LinkDatabase.GetReferrers(item).ToList().Select(x => x.GetSourceItem());

                                var first = true;
                                var data = "";
                                foreach (var referrer in referrers)
                                {
                                    if (referrer != null)
                                    {
                                        if (!first)
                                        {
                                            data += ";\n";
                                        }
                                        data += referrer.Paths.ContentPath;
                                        first = false;
                                    }
                                }
                                itemLine += "\"" + data + "\"\t";

                            }

                            foreach (var field in fields)
                            {
                                if (!String.IsNullOrEmpty(field))
                                {
                                    var fieldName = GetFieldNameIfGuid(field);
                                    var itemField = item.Fields[field];
                                    if (itemField == null)
                                    {
                                        itemLine += String.Format("n/a\t{0}-ID{0}-HTML", fieldName);
                                    }
                                    else
                                    {
                                        var itemOfType = FieldTypeManager.GetField(itemField);
                                        if (itemOfType is ImageField) // if image field
                                        {
                                            ImageField imageField = itemField;
                                            if (includeLinkedIds)
                                            {
                                                headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Format("{0} ID\t", fieldName));
                                            }
                                            if (includeRawHtml)
                                            {
                                                headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Format("{0} Raw HTML\t", fieldName));
                                            }
                                            if (imageField == null)
                                            {
                                                itemLine += "n/a\t";

                                                if (includeLinkedIds)
                                                {
                                                    itemLine += "n/a\t";
                                                }

                                                if (includeRawHtml)
                                                {
                                                    itemLine += "n/a\t";
                                                }
                                            }
                                            else if (imageField.MediaItem == null)
                                            {

                                                itemLine += "\t";
                                                if (includeLinkedIds)
                                                {
                                                    itemLine += "\t";
                                                }

                                                if (includeRawHtml)
                                                {
                                                    itemLine += "\t";
                                                }
                                            }
                                            else
                                            {
                                                itemLine += imageField.MediaItem.Paths.MediaPath + "\t";
                                                if (includeLinkedIds)
                                                {
                                                    itemLine += imageField.MediaItem.ID + "\t";
                                                }

                                                if (includeRawHtml)
                                                {
                                                    itemLine += imageField.Value + "\t";
                                                }
                                            }
                                        }
                                        else if (itemOfType is LinkField)
                                        {
                                            LinkField linkField = itemField;
                                            if (includeLinkedIds)
                                            {
                                                headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Empty);
                                            }
                                            if (includeRawHtml)
                                            {
                                                headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Format("{0} Raw HTML\t", fieldName));
                                            }
                                            if (linkField == null)
                                            {
                                                itemLine += "n/a\t";

                                                if (includeRawHtml)
                                                {
                                                    itemLine += "n/a\t";
                                                }
                                            }
                                            else
                                            {
                                                itemLine += linkField.Url + "\t";

                                                if (includeRawHtml)
                                                {
                                                    itemLine += linkField.Value + "\t";
                                                }
                                            }
                                        }
                                        else if (itemOfType is ReferenceField || itemOfType is GroupedDroplistField || itemOfType is LookupField)
                                        {
                                            ReferenceField refField = itemField;
                                            if (includeLinkedIds)
                                            {
                                                headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Format("{0} ID\t", fieldName));
                                            }
                                            if (includeRawHtml)
                                            {
                                                headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Empty);
                                            }
                                            if (refField == null)
                                            {
                                                itemLine += "n/a\t";
                                                if (includeLinkedIds)
                                                {
                                                    itemLine += "n/a\t";
                                                }
                                            }
                                            else if (refField.TargetItem == null)
                                            {
                                                itemLine += "\t";
                                                if (includeLinkedIds)
                                                {
                                                    itemLine += "\t";
                                                }
                                            }
                                            else
                                            {
                                                itemLine += refField.TargetItem.DisplayName + "\t";
                                                if (includeLinkedIds)
                                                {
                                                    itemLine += refField.TargetID + "\t";
                                                }
                                            }
                                        }
                                        else if (itemOfType is MultilistField)
                                        {
                                            MultilistField multiField = itemField;
                                            if (includeLinkedIds)
                                            {
                                                headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Format("{0} ID\t", fieldName));
                                            }
                                            if (includeRawHtml)
                                            {
                                                headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Empty);
                                            }
                                            if (multiField == null)
                                            {
                                                itemLine += "n/a\t";
                                                if (includeLinkedIds)
                                                {
                                                    itemLine += "n/a\t";
                                                }
                                            }
                                            else
                                            {
                                                var multiItems = multiField.GetItems();
                                                var data = "";
                                                var first = true;
                                                foreach (var i in multiItems)
                                                {
                                                    if (!first)
                                                    {
                                                        data += "\n";
                                                    }
                                                    var url = i.Paths.ContentPath;
                                                    data += url + ";";
                                                    first = false;
                                                }
                                                itemLine += "\"" + data + "\"" + "\t";

                                                if (includeLinkedIds)
                                                {
                                                    first = true;
                                                    var idData = "";
                                                    foreach (var i in multiItems)
                                                    {
                                                        if (!first)
                                                        {
                                                            idData += "\n";
                                                        }
                                                        idData += i.ID + ";";
                                                        first = false;
                                                    }
                                                    itemLine += "\"" + idData + "\"" + "\t";
                                                }
                                            }
                                        }
                                        else if (itemOfType is CheckboxField)
                                        {
                                            CheckboxField checkboxField = itemField;
                                            headingString = headingString.Replace(String.Format("{0}-ID", fieldName), string.Empty).Replace(String.Format("{0}-HTML", fieldName), string.Empty);
                                            if (itemField == null)
                                            {
                                                itemLine += "n/a\t";
                                            }
                                            else
                                            {
                                                itemLine += checkboxField.Checked.ToString() + "\t";
                                            }
                                        }
                                        else // default text field
                                        {
                                            itemLine += RemoveLineEndings(itemField.Value) + "\t";
                                            headingString = headingString.Replace(String.Format("{0}-ID", fieldName), string.Empty).Replace(String.Format("{0}-HTML", fieldName), string.Empty);
                                        }
                                    }
                                }
                            }                            

                            dataLines.Add(itemLine);
                        }
                    }

                    headingString += (!chkAllFields.Checked ? GetExcelHeaderForFields(fields, includeLinkedIds, includeRawHtml) : GetExcelHeaderForFields(fields, false, false));


                    // remove any field-ID and field-RAW from header that haven't been replaced (i.e. non-existent field)
                    foreach (var field in fields)
                    {
                        var fieldName = GetFieldNameIfGuid(field);
                        headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Empty);
                        headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Empty);
                    }

                    sw.WriteLine(headingString);
                    foreach (var line in dataLines)
                    {
                        var newLine = line;
                        foreach (var field in fields)
                        {
                            var fieldName = GetFieldNameIfGuid(field);
                            newLine = newLine.Replace(String.Format("{0}-ID", fieldName), headingString.Contains(String.Format("{0} ID", fieldName)) ? "n/a\t" : string.Empty);
                            newLine = newLine.Replace(String.Format("{0}-HTML", fieldName), headingString.Contains(String.Format("{0} Raw HTML", fieldName)) ? "n/a\t" : string.Empty);
                        }
                        sw.WriteLine(newLine);
                    }

                    var downloadToken = txtDownloadToken.Value;     
                    var responseCookie = new HttpCookie("DownloadToken");
                    responseCookie.Value = downloadToken;
                    responseCookie.Expires = DateTime.Now.AddDays(1);
                    Response.Cookies.Add(responseCookie);   

                    Response.Output.Write(sw.ToString());
                    Response.Flush();
                    Response.End();                
                }
            }
            catch (Exception ex)
            {
                litFeedback.Text = ex.Message;
            }
        }

        public List<Item> GetItems()
        {
            var startNode = inputStartitem.Value;
            if (String.IsNullOrEmpty(startNode)) startNode = "/sitecore/content";

            var templateString = inputTemplates.Value;
            var templates = templateString.ToLower().Split(',').Select(x => x.Trim()).ToList();
            var fastQuery = txtFastQuery.Value;

            var exportItems = new List<Item>();
            if (!String.IsNullOrEmpty(fastQuery))
            {
                var queryItems = _db.SelectItems(fastQuery);
                exportItems = queryItems.ToList();
            }
            else
            {
                Item startItem = _db.GetItem(startNode);
                var descendants = startItem.Axes.GetDescendants();
                exportItems.Add(startItem);
                exportItems.AddRange(descendants);
            }

            if (!String.IsNullOrEmpty(inputMultiStartItem.Value))
            {
                var startItems = inputMultiStartItem.Value.Split(',');
                foreach (var startItem in startItems)
                {
                    Item item = _db.GetItem(startItem);
                    if (item != null)
                    {
                        var descendants = item.Axes.GetDescendants();
                        exportItems.Add(item);
                        exportItems.AddRange(descendants);
                    }
                }
            } 
            var items = new List<Item>();
            if (!String.IsNullOrEmpty(templateString))
            {
                foreach (var template in templates)
                {
                    var templateItems = exportItems.Where(x => x.TemplateName.ToLower() == template || x.TemplateID.ToString().ToLower().Replace("{", string.Empty).Replace("}", string.Empty) == template.Replace("{", string.Empty).Replace("}", string.Empty));
                    items.AddRange(templateItems);
                }
            }
            else
            {
                items = exportItems.ToList();
            }

            if (chkItemsWithLayout.Checked)
            {
                items = items.Where(DoesItemHasPresentationDetails).ToList();
            }
            return items;
        }

        public bool DoesItemHasPresentationDetails(Item item)
        {
            if (item != null)
            {
                return item.Fields[Sitecore.FieldIDs.LayoutField] != null
                       && !String.IsNullOrEmpty(item.Fields[Sitecore.FieldIDs.LayoutField].Value);
            }
            return false;
        }

        public string GetFieldNameIfGuid(string field)
        {
            Guid guid;
            if (Guid.TryParse(field, out guid))
            {
                var fieldItem = _db.GetItem(field);
                if (fieldItem == null) return field;
                return fieldItem.Name;
            }
            else
            {
                return field;
            }
        }

        public string GetExcelHeaderForFields(IEnumerable<string> fields, bool includeId = false, bool includeRaw = false)
        {
            var header = "";
            foreach (var field in fields)
            {
                var fieldName = GetFieldNameIfGuid(field);

                header += fieldName + "\t";

                if (includeId)
                {
                    header += String.Format("{0}-ID", fieldName);
                }

                if (includeRaw)
                {
                    header += String.Format("{0}-HTML", fieldName);
                }
            }
            return header;
        }

        public string RemoveLineEndings(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            return value.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(lineSeparator, string.Empty).Replace(paragraphSeparator, string.Empty).Replace("<br/>", string.Empty).Replace("<br />", string.Empty).Replace("\t", "   ");
        }     

        protected string FormatGuidForHtmlId(ID id)
        {
            return id.ToString().Replace("{", string.Empty).Replace("}", string.Empty).Replace("-", string.Empty);
        }

        protected string GetFieldLink(Item field)
        {
            ReferenceField fieldLink = field.Fields["Field Link"];
            if (fieldLink == null || fieldLink.TargetItem == null) return string.Empty;

            var val = fieldLink.TargetItem.DisplayName;
            var splitval = val.Split('/');
            return splitval[splitval.Length - 1];
        }

        protected void btnTestFastQuery_OnClick(object sender, EventArgs e)
        {
            HideModals(false, false, false);
            if (!SetDatabase()) SetDatabase("web");

            var fastQuery = txtFastQuery.Value;
            if (String.IsNullOrEmpty(fastQuery)) return;

            try
            {
                var results = _db.SelectItems(fastQuery);
                if (results == null)
                {
                    litFastQueryTest.Text = "Query returned null";
                }
                else
                {
                    litFastQueryTest.Text = String.Format("Query returned {0} items", results.Length);
                }
            }
            catch (Exception ex)
            {
                litFastQueryTest.Text = "Error: " + ex.Message;
            }

        }

        public class SitecoreItemApiResponse
        {
            public int statusCode { get; set; }
        }

        protected void CheckSitecoreItemApiEnabled()
        {
            _sitecoreItemApiEnabled = false;
            try
            {
                var current = HttpContext.Current.Request.Url;
                var root = current.Scheme + "://" + current.Host;
                var apiUrl = root + "/-/item/v1/?sc_itemid={00000000-0000-0000-0000-000000000000}";
                WebRequest request = WebRequest.Create(apiUrl);
                var response = (HttpWebResponse) request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        var obj = reader.ReadToEnd();
                        SitecoreItemApiResponse apiResponse =
                            (SitecoreItemApiResponse) js.Deserialize(obj, typeof (SitecoreItemApiResponse));

                        if (apiResponse.statusCode == 200)
                        {
                            _sitecoreItemApiEnabled = true;
                        }
                    }
                }
                response.Close();
            }
            catch (Exception ex)
            {
                
            }
                     
        }

        protected void HideModals(bool hideBrowse, bool hideTemplates, bool hideFields)
        {
            PhBrowseTree.Visible = hideBrowse;
            PhBrowseFields.Visible = hideTemplates;
            PhBrowseTemplates.Visible = hideFields;
        }

        protected void btnBrowse_OnClick(object sender, EventArgs e)
        {
            litSitecoreContentTree.Text = GetSitecoreTreeHtml();
            PhBrowseTree.Visible = true;
            PhBrowseFields.Visible = false;
            PhBrowseTemplates.Visible = false;
        }

        protected void btnBrowseTemplates_OnClick(object sender, EventArgs e)
        {
            litBrowseTemplates.Text = GetAvailableTemplates();
            PhBrowseTemplates.Visible = true;
            PhBrowseFields.Visible = false;
            PhBrowseTree.Visible = false;
        }

        protected void btnBrowseFields_OnClick(object sender, EventArgs e)
        {
            litBrowseFields.Text = GetAvailableFields();
            PhBrowseFields.Visible = true;
            PhBrowseTree.Visible = false;
            PhBrowseTemplates.Visible = false;
        }

        protected SettingsList ReadSettingsFromFile()
        {
            var serializer = new JavaScriptSerializer();

            if (!File.Exists(_settingsFilePath))
            {
                return null;
            }

            var fileContents = File.ReadAllText(_settingsFilePath);
            // convert into a list of settings
            var settingsList = serializer.Deserialize<SettingsList>(fileContents);

            // get settings that belong to current user
            var userId = GetUserId();
            if (userId != null)
            {
                settingsList.Settings = settingsList.Settings.Where(x => String.IsNullOrEmpty(x.UserId) || x.UserId == userId).ToList();
            }

            return settingsList;
        }

        protected string GetUserId()
        {
            var user = Sitecore.Security.Accounts.User.Current;
            if (user != null && user.Profile != null)
            {
                return user.Profile.UserName;
            }
            return null;
        }

        protected void btnSaveSettings_OnClick(object sender, EventArgs e)
        {
            PhBrowseFields.Visible = false;
            PhBrowseTemplates.Visible = false;
            PhBrowseTree.Visible = false;

            var saveName = txtSaveSettingsName.Value;

            var settingsData = new ExportSettingsData()
            {
                Database = ddDatabase.SelectedValue,
                IncludeIds = chkIncludeIds.Checked,
                StartItem = inputStartitem.Value,
                FastQuery = txtFastQuery.Value,
                Templates = inputTemplates.Value,
                IncludeTemplateName = chkIncludeTemplate.Checked,
                Fields = inputFields.Value,
                IncludeLinkedIds = chkIncludeLinkedIds.Checked,
                IncludeRaw = chkIncludeRawHtml.Checked,
                Workflow = chkWorkflowName.Checked,
                WorkflowState = chkWorkflowState.Checked,
                SelectedLanguage = ddLanguages.SelectedValue,
                GetAllLanguages = chkAllLanguages.Checked,
                IncludeName = chkIncludeName.Checked,
                IncludeInheritance = chkIncludeInheritance.Checked,
                MultipleStartPaths = inputMultiStartItem.Value,
                DateCreated = chkDateCreated.Checked,
                DateModified = chkDateModified.Checked,
                CreatedBy = chkCreatedBy.Checked,
                ModifiedBy = chkModifiedBy.Checked,
                NeverPublish = chkNeverPublish.Checked,
                RequireLayout = chkItemsWithLayout.Checked,
                Referrers = chkReferrers.Checked,
                FileName = txtFileName.Value,
                AllFields = chkAllFields.Checked
            };

            var settingsObject = new ExportSettings()
            {
                Name = saveName,
                Data = settingsData,
                UserId = GetUserId()
            };

            var serializer = new JavaScriptSerializer();

            var savedSettings = ReadSettingsFromFile();

            if (savedSettings == null)
            {
                var settingsList = new SettingsList();
                settingsList.Settings = new List<ExportSettings>()
                {
                    settingsObject
                };
                var settingsJson = serializer.Serialize(settingsList);
                File.WriteAllText(_settingsFilePath, settingsJson);
            }
            else
            {
                if (savedSettings.Settings.Any(x => x.Name == saveName))
                {
                    phOverwriteScript.Visible = true;
                    return;
                }

                savedSettings.Settings.Insert(0, settingsObject);
                var settingsListJson = serializer.Serialize(savedSettings);
                File.WriteAllText(_settingsFilePath, settingsListJson);
            }
            litSavedMessage.Text = "Saved!";
            SetSavedSettingsDropdown();
            ddSavedSettings.SelectedValue = saveName;
        }

        public class SettingsList
        {
            public List<ExportSettings> Settings;
        }

        public class ExportSettings
        {
            public string UserId;
            public string Name;
            public ExportSettingsData Data;
        }

        public class ExportSettingsData
        {
            public string Database;
            public bool IncludeIds;
            public string StartItem;
            public string FastQuery;
            public string Templates;
            public bool IncludeTemplateName;
            public string Fields;
            public bool IncludeLinkedIds;
            public bool IncludeRaw;
            public bool Workflow;
            public bool WorkflowState;
            public string SelectedLanguage;
            public bool GetAllLanguages;
            public bool IncludeName;
            public string MultipleStartPaths;
            public bool IncludeInheritance;
            public bool NeverPublish;
            public bool DateCreated;
            public bool DateModified;
            public bool CreatedBy;
            public bool ModifiedBy;
            public bool RequireLayout;
            public bool Referrers;
            public string FileName;
            public bool AllFields;
        }

        protected void ddSavedSettings_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            var settingsName = ddSavedSettings.SelectedValue;
            if (String.IsNullOrEmpty(settingsName))
            {
                btnDeletePrompt.Visible = false;
                ClearAll();
                return;
            }
            btnDeletePrompt.Visible = true;
            var savedSettings = ReadSettingsFromFile();
            if (savedSettings == null) return;
            var selectedSettings = savedSettings.Settings.FirstOrDefault(x => x.Name == settingsName);
            var settings = selectedSettings.Data;

            if (!String.IsNullOrEmpty(settings.Database))
            {
                ddDatabase.SelectedValue = settings.Database;
            }
            chkIncludeIds.Checked = settings.IncludeIds;
            inputStartitem.Value = settings.StartItem;
            txtFastQuery.Value = settings.FastQuery;
            inputTemplates.Value = settings.Templates;
            chkIncludeTemplate.Checked = settings.IncludeTemplateName;
            inputFields.Value = settings.Fields;
            chkIncludeLinkedIds.Checked = settings.IncludeLinkedIds;
            chkIncludeRawHtml.Checked = settings.IncludeRaw;
            chkWorkflowName.Checked = settings.Workflow;
            chkWorkflowState.Checked = settings.WorkflowState;

            var languages = GetSiteLanguages();
            if (languages.Any(x => x.GetDisplayName() == settings.SelectedLanguage))
            {
                ddLanguages.SelectedValue = settings.SelectedLanguage;
            }
            chkAllLanguages.Checked = settings.GetAllLanguages;
            chkIncludeName.Checked = settings.IncludeName;
            chkIncludeInheritance.Checked = settings.IncludeInheritance;
            inputMultiStartItem.Value = settings.MultipleStartPaths;
            chkDateCreated.Checked = settings.DateCreated;
            chkDateModified.Checked = settings.DateModified;
            chkCreatedBy.Checked = settings.CreatedBy;
            chkModifiedBy.Checked = settings.ModifiedBy;
            chkNeverPublish.Checked = settings.NeverPublish;
            chkItemsWithLayout.Checked = settings.RequireLayout;
            chkReferrers.Checked = settings.Referrers;
            txtFileName.Value = settings.FileName;
            chkAllFields.Checked = settings.AllFields;
        }

        protected void btnClearAll_OnClick(object sender, EventArgs e)
        {
            ClearAll();
        }

        protected void ClearAll()
        {
            chkIncludeIds.Checked = false;
            inputStartitem.Value = string.Empty;
            txtFastQuery.Value = string.Empty;
            inputTemplates.Value = string.Empty;
            chkIncludeTemplate.Checked = false;
            inputFields.Value = string.Empty;
            chkIncludeLinkedIds.Checked = false;
            chkIncludeRawHtml.Checked = false;
            chkWorkflowName.Checked = false;
            chkWorkflowState.Checked = false;
            ddLanguages.SelectedIndex = 0;
            chkAllLanguages.Checked = false;
            txtSaveSettingsName.Value = string.Empty;
            ddSavedSettings.SelectedIndex = 0;
            chkItemsWithLayout.Checked = false;
            chkIncludeInheritance.Checked = false;
            chkDateCreated.Checked = false;
            chkDateModified.Checked = false;
            chkCreatedBy.Checked = false;
            chkModifiedBy.Checked = false;
            inputMultiStartItem.Value = string.Empty;
            chkIncludeName.Checked = false;
            chkReferrers.Checked = false;
            txtFileName.Value = string.Empty;
            chkAllFields.Checked = false;
        }

        protected IEnumerable<string> LineParser(string line)
        {
            int fieldStart = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ',')
                {
                    yield return line.Substring(fieldStart, i - fieldStart);
                    fieldStart = i + 1;
                }
                if (line[i] == '"')
                    for (i++; line[i] != '"'; i++) { }
            }
        }


        protected void btnOverWriteSettings_OnClick(object sender, EventArgs e)
        {
            var saveName = txtSaveSettingsName.Value;

            var settingsData = new ExportSettingsData()
            {
                Database = ddDatabase.SelectedValue,
                IncludeIds = chkIncludeIds.Checked,
                StartItem = inputStartitem.Value,
                FastQuery = txtFastQuery.Value,
                Templates = inputTemplates.Value,
                IncludeTemplateName = chkIncludeTemplate.Checked,
                Fields = inputFields.Value,
                IncludeLinkedIds = chkIncludeLinkedIds.Checked,
                IncludeRaw = chkIncludeRawHtml.Checked,
                Workflow = chkWorkflowName.Checked,
                WorkflowState = chkWorkflowState.Checked,
                SelectedLanguage = ddLanguages.SelectedValue,
                GetAllLanguages = chkAllLanguages.Checked,
                IncludeName  = chkIncludeName.Checked,
                IncludeInheritance = chkIncludeInheritance.Checked,
                MultipleStartPaths = inputMultiStartItem.Value,
                DateCreated = chkDateCreated.Checked,
                DateModified = chkDateModified.Checked,
                CreatedBy = chkCreatedBy.Checked,
                ModifiedBy = chkModifiedBy.Checked,
                NeverPublish = chkNeverPublish.Checked,
                RequireLayout = chkItemsWithLayout.Checked,
                Referrers = chkReferrers.Checked,
                FileName = txtFileName.Value
            };

            var serializer = new JavaScriptSerializer();

            var savedSettings = ReadSettingsFromFile();

            var setting = savedSettings.Settings.FirstOrDefault(x => x.Name == saveName);

            if (setting == null) return;
            setting.Data = settingsData;
            var settingsListJson = serializer.Serialize(savedSettings);
            File.WriteAllText(_settingsFilePath, settingsListJson);

            litSavedMessage.Text = "Saved!";
            SetSavedSettingsDropdown();
            ddSavedSettings.SelectedValue = saveName;
        }

        protected void btnDeleteSavedSetting_OnClick(object sender, EventArgs e)
        {
            var settingsName = ddSavedSettings.SelectedValue;
            var savedSettings = ReadSettingsFromFile();

            var setting = savedSettings.Settings.FirstOrDefault(x => x.Name == settingsName);
            var serializer = new JavaScriptSerializer();
            if (setting != null)
            {
                savedSettings.Settings.Remove(setting);
                var settingsListJson = serializer.Serialize(savedSettings);
                File.WriteAllText(_settingsFilePath, settingsListJson);
                SetSavedSettingsDropdown();
            }
        }

        protected void btnAdvancedSearch_OnClick(object sender, EventArgs e)
        {
            HideModals(false, false, false);
            if (!SetDatabase()) SetDatabase("web");

            var searchText = txtAdvancedSearch.Value;
            Response.Clear();
            Response.Buffer = true;
            var fileName = !String.IsNullOrEmpty(txtFileName.Value) ? txtFileName.Value : "ContentSearch - " + searchText;
            Response.AddHeader("content-disposition", String.Format("attachment;filename={0}.xls", fileName));
            Response.Charset = "";
            Response.ContentType = "application/vnd.ms-excel";

            var items = GetItems();

            using (StringWriter sw = new StringWriter())
            {
                var headingString = "Item Path\tField";
                var addedLangToHeading = false;
                                    

                var dataLines = new List<string>();

                foreach (var baseItem in items)
                {
                    var itemVersions = new List<Item>();
                    foreach (var language in baseItem.Languages)
                    {
                        var languageItem = baseItem.Database.GetItem(baseItem.ID, language);
                        if (languageItem.Versions.Count > 0)
                        {
                            itemVersions.Add(languageItem);
                        }
                    }

                    foreach (var version in itemVersions)
                    {
                        // check for string in all fields
                        // if string is found, add to export with field where it exists
                        var fieldsWithText = CheckAllFields(version, searchText);
                        if (!String.IsNullOrEmpty(fieldsWithText))
                        {
                            var dataLine = baseItem.Paths.ContentPath + "\t" + fieldsWithText;
                            if (version.Language.Name != LanguageManager.DefaultLanguage.Name)
                            {
                                dataLine += "\t" + version.Language.GetDisplayName();
                                if (!addedLangToHeading)
                                {
                                    addedLangToHeading = true;
                                    headingString += "\tLanguage";
                                }
                            }
                            dataLines.Add(dataLine);
                        }
                    }
                }

                sw.WriteLine(headingString);
                foreach (var line in dataLines)
                {
                    sw.WriteLine(line);
                }

                var downloadToken = txtDownloadToken.Value;
                var responseCookie = new HttpCookie("DownloadToken");
                responseCookie.Value = downloadToken;
                responseCookie.Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Add(responseCookie);

                Response.Output.Write(sw.ToString());
                Response.Flush();
                Response.End();
            }
        }

        protected string CheckAllFields(Item dataItem, string searchText)
        {
            searchText = searchText.ToLower();
            //Force all the fields to load.
            dataItem.Fields.ReadAll();

            var fieldsWithText = "";

            //Loop through all of the fields in the datasource item looking for
            //text in non system fields
            foreach (Field field in dataItem.Fields)
            {
                //If a field starts with __ it means it is a sitecore system
                //field which we do not want to index.
                if (field.Name.StartsWith("__"))
                {
                    continue;
                }

                //Only add text based fields.
                if (FieldTypeManager.GetField(field) is HtmlField)
                {
                    var html = field.Value.ToLower();
                    if (html.Contains(searchText))
                    {
                        if (!String.IsNullOrEmpty(fieldsWithText)) fieldsWithText += "; ";
                        fieldsWithText += field.Name;
                    }
                }

                //Add the field text to the overall searchable text.
                if (FieldTypeManager.GetField(field) is TextField)
                {
                    if (field.Value.ToLower().Contains(searchText))
                    {
                        if (!String.IsNullOrEmpty(fieldsWithText)) fieldsWithText += "; ";
                        fieldsWithText += field.Name;
                    }
                }

                // droplist, treelist
                if (FieldTypeManager.GetField(field) is LookupField)
                {
                    var lookupField = (LookupField)field;
                    var tagName = GetTagName(lookupField.TargetItem);
                    if (!String.IsNullOrEmpty(tagName) && tagName.ToLower().Contains(searchText))
                    {
                        if (!String.IsNullOrEmpty(fieldsWithText)) fieldsWithText += "; ";
                        fieldsWithText += field.Name;
                    }
                }

                else if (field.Type == "TreelistEx")
                {
                    var treelistField = (MultilistField)field;
                    var fieldItems = treelistField.GetItems();

                    foreach (var item in fieldItems)
                    {
                        var tagName = GetTagName(item);
                        if (!String.IsNullOrEmpty(tagName) && tagName.ToLower().Contains(searchText))
                        {
                            if (!String.IsNullOrEmpty(fieldsWithText)) fieldsWithText += "; ";
                            fieldsWithText += field.Name;
                        }
                    }
                }

                else
                {
                    var ids = field.Value?.Split('|').Where(x => !String.IsNullOrEmpty(x)).ToList();
                    if (ids.Any())
                    {
                        foreach (var id in ids)
                        {
                            var item = _db.GetItem(id);
                            if (item != null)
                            {
                                var tagName = GetTagName(item);
                                if (tagName.ToLower().Contains(searchText))
                                {
                                    if (!String.IsNullOrEmpty(fieldsWithText)) fieldsWithText += "; ";
                                    fieldsWithText += field.Name;
                                }
                            }
                        }
                    }
                }
            }
            return fieldsWithText;
        }

        public string GetTagName(Item item)
        {
            return !String.IsNullOrEmpty(item?.Fields["Title"]?.Value)
                                ? item.Fields["Title"].Value
                                : item?.Name;
        }
    }
}
