using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using ImageField = Sitecore.Data.Fields.ImageField;

namespace ContentExportTool
{
    public partial class ContentExport : Sitecore.sitecore.admin.AdminPage
    {
        #region Construction 

        private Database _db;
        private string _settingsItemPath = "/sitecore/system/Modules/Content Export Tool/Saved Settings";
        private List<FieldData> _fieldsList;

        protected void Page_Load(object sender, EventArgs e)
        {
            divAdvOptions.Attributes["class"] = "advanced";
            litUploadResponse.Text = String.Empty;
            litFeedback.Text = String.Empty;
            var dbName = (!String.IsNullOrEmpty(ddDatabase.SelectedValue) ? ddDatabase.SelectedValue : "master");
            _db = Sitecore.Configuration.Factory.GetDatabase(dbName);
            litSavedMessage.Text = String.Empty;
            phOverwriteScript.Visible = false;
            phDeleteScript.Visible = false;
            litFastQueryTest.Text = String.Empty;
            if (!IsPostBack)
            {
                if (!String.IsNullOrEmpty(Request.QueryString["getitems"]) &&
                    !String.IsNullOrEmpty(Request.QueryString["startitem"]))
                {
                    GetItemsAsync(Request.QueryString["startitem"]);
                }else if (!String.IsNullOrEmpty(Request.QueryString["getfields"]) &&
                          !String.IsNullOrEmpty(Request.QueryString["startitem"]))
                {
                    GetFieldsAsync(Request.QueryString["startitem"]);
                }
                SetupForm();
            }

            // check if advanced options should be open
            if (OpenAdvancedOptions())
            {
                divAdvOptions.Attributes["class"] = "advanced open open-default";
            }
        }

        protected void SetupForm()
        {
            chkNoDuplicates.Checked = true;
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

            radDateRangeAnd.Checked = false;
            radDateRangeOr.Checked = true;

            SetSavedSettingsDropdown();
        }

        protected bool OpenAdvancedOptions()
        {
            return (!String.IsNullOrEmpty(txtAdvancedSearch.Value) ||
                    !String.IsNullOrEmpty(txtStartDateCr.Value) ||
                    !String.IsNullOrEmpty(txtEndDateCr.Value) ||
                    !String.IsNullOrEmpty(txtStartDatePb.Value) ||
                    !String.IsNullOrEmpty(txtEndDatePu.Value) ||
                    !String.IsNullOrEmpty(inputMultiStartItem.Value) ||
                    !String.IsNullOrEmpty(txtFileName.Value) ||
                    chkIncludeIds.Checked ||
                    chkIncludeRawHtml.Checked ||
                    chkReferrers.Checked ||
                    chkDateCreated.Checked ||
                    chkDateModified.Checked ||
                    chkCreatedBy.Checked ||
                    chkModifiedBy.Checked ||
                    chkNeverPublish.Checked ||
                    chkWorkflowName.Checked ||
                    chkWorkflowState.Checked ||
                    chkAllLanguages.Checked ||
                    ddLanguages.SelectedIndex != 0
                );
        }

        protected List<Language> GetSiteLanguages()
        {
            var database = ddDatabase.SelectedValue;
            SetDatabase(database);
            var installedLanguages = LanguageManager.GetLanguages(_db);

            return installedLanguages.ToList();
        }

        protected void SetSavedSettingsDropdown(bool allUsers = false)
        {
            var settings = new List<ExportSettings>();
            
            var savedSettings = ReadSettingsFromFile(allUsers);

            if (savedSettings != null && savedSettings.Settings != null)
            {
                settings = savedSettings.Settings;
            }

            foreach (var setting in settings)
            {
                if (setting.UserId != GetUserId())
                {
                    setting.Name += " [" + setting.UserId + "]";
                }
            }

            settings.Insert(0, new ExportSettings()
            {
                Data = null,
                ID = "",
                Name = "",
                UserId = ""
            });
                     
            ddSavedSettings.DataSource = settings;
            ddSavedSettings.DataValueField = "ID";
            ddSavedSettings.DataTextField = "Name";
            ddSavedSettings.DataBind();
        }

        protected override void OnInit(EventArgs e)
        {
            base.CheckSecurity(true); //Required!
            base.OnInit(e);
        }

        #endregion

        #region Browse

        public void GetItemsAsync(string startItem)
        {
            if (_db == null) _db = Sitecore.Configuration.Factory.GetDatabase("master");
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            var item = _db.GetItem(startItem);
            var children = item.Children;

            var returnItems = children.Select(x => new BrowseItem()
            {
                Id = x.ID.ToString(),
                Name = x.DisplayName,
                Path = x.Paths.FullPath,
                HasChildren = x.HasChildren,
                Template = x.TemplateName
            });

            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(returnItems);
            Response.Write(json);
            Response.End();
        }

        public void GetFieldsAsync(string startItem)
        {
            if (_db == null) _db = Sitecore.Configuration.Factory.GetDatabase("master");
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";

            var templateItem = _db.GetTemplate(startItem);
            var fields = templateItem.Fields.Where(x => !x.Name.StartsWith("__"));

            var returnItems = fields.Select(x => new BrowseItem()
            {
                Id = x.ID.ToString(),
                Name = x.DisplayName,
                Path = "",
                HasChildren = false,
                Template = "Field"
            });

            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(returnItems);
            Response.Write(json);
            Response.End();
        }

        protected void btnBrowse_OnClick(object sender, EventArgs e)
        {
            litSitecoreContentTree.Text = GetSitecoreTreeHtml();
            PhBrowseTree.Visible = true;
            PhBrowseFields.Visible = false;
            PhBrowseTemplates.Visible = false;
        }

        protected string GetSitecoreTreeHtml()
        {
            var database = ddDatabase.SelectedValue;
            SetDatabase(database);
            var contentRoot = _db.GetItem("/sitecore");

            var sitecoreTreeHtml = "<ul>";
            sitecoreTreeHtml += GetItemAndChildren(contentRoot);
            sitecoreTreeHtml += "</ul>";

            return sitecoreTreeHtml;
        }

        protected string GetItemAndChildren(Item item)
        {
            var children = item.GetChildren().Cast<Item>();

            StringBuilder nodeHtml = new StringBuilder();
            nodeHtml.Append("<li data-name='" + item.Name.ToLower() + "' data-id='" + item.ID + "'>");
            if (children.Any())
            {
                nodeHtml.Append("<a class='browse-expand' onclick='expandNode($(this))'>+</a>");
            }
            nodeHtml.AppendFormat("<a class='sitecore-node' href='javascript:void(0)' ondblclick='selectNode($(this));addTemplate();' onclick='selectNode($(this));' data-path='{0}' data-name='{1}' data-id='{2}'>{1}</a>", item.Paths.Path, String.IsNullOrEmpty(item.DisplayName) ? item.Name : item.DisplayName, item.ID.ToString());

            nodeHtml.Append("</li>");

            return nodeHtml.ToString();
        }

        protected string GetChildList(IEnumerable<Item> children)
        {
            // turn on notification message
            if (!children.Any())
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append("<ul>");
            foreach (Item child in children)
            {
                sb.Append(GetItemAndChildren(child));
            }
            sb.Append("</ul>");

            return sb.ToString();
        }

        #endregion

        #region Browse Templates

        protected void btnBrowseTemplates_OnClick(object sender, EventArgs e)
        {
            litBrowseTemplates.Text = GetAvailableTemplates();
            PhBrowseTemplates.Visible = true;
            PhBrowseFields.Visible = false;
            PhBrowseTree.Visible = false;
        }

        protected string GetAvailableTemplates()
        {
            SetDatabase("master");
            var startItem = _db.GetItem("/sitecore/templates");

            StringBuilder html = new StringBuilder("<ul>");
            html.Append(GetTemplateTree(startItem));
            html.Append("</ul>");

            return html.ToString();
        }

        protected string GetTemplateTree(Item item)
        {
            var children = item.GetChildren();

            StringBuilder nodeHtml = new StringBuilder();
            nodeHtml.Append("<li data-name='" + item.Name.ToLower() + "' data-id='" + item.ID + "'>");
            if (item.TemplateName == "Template")
            {
                nodeHtml.AppendFormat(
                        "<a data-id='{0}' data-name='{1}' class='template-link' href='javascript:void(0)' onclick='selectBrowseNode($(this));' ondblclick='selectBrowseNode($(this));addTemplate();'>{1}</a>",
                        item.ID, item.Name);
            }
            else
            {
                if (children.Any())
                {
                    nodeHtml.Append("<a class='browse-expand' onclick='expandNode($(this))'>+</a><span></span>");
                }
                nodeHtml.AppendFormat("<span>{0}</span>", item.Name);
            }
            nodeHtml.Append("</li>");

            return nodeHtml.ToString();
        }

        protected string GetChildTemplateList(ChildList children)
        {
            // turn on notification message
            if (!children.Any())
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            sb.Append("<ul>");
            foreach (Item child in children)
            {
                sb.Append(GetTemplateTree(child));
            }
            sb.Append("</ul>");

            return sb.ToString();
        }

        #endregion

        #region Browse Fields

        protected void btnBrowseFields_OnClick(object sender, EventArgs e)
        {
            litBrowseFields.Text = GetAvailableFields();
            PhBrowseFields.Visible = true;
            PhBrowseTree.Visible = false;
            PhBrowseTemplates.Visible = false;
        }

        protected string GetAvailableFields()
        {
            SetDatabase("master");

            string html = "<ul>";

            var templateList = new List<TemplateItem>();
            var startItem = _db.GetItem("/sitecore/templates");
            if (!string.IsNullOrWhiteSpace(inputTemplates.Value))
            {
                IEnumerable<Item> allTemplates = null;
                var templateNames = inputTemplates.Value.Split(',');
                foreach (var templateName in templateNames.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var name = templateName.Trim().ToLower();
                    // try get as path or guid
                    var template = _db.GetItem(name);
                    // try get by name
                    if (template == null)
                    {
                        if (allTemplates == null)
                        {
                            allTemplates = startItem.Axes.GetDescendants();
                        }
                        template = allTemplates.Where(x => x.TemplateName == "Template").FirstOrDefault(x => x.Name.ToLower() == name);
                    }

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
                fields = fields.OrderBy(x => x.Name).Where(x => !String.IsNullOrEmpty(x.Name));
                if (fields.Any())
                {
                    html += "<li data-id='" + template.ID + "' data-name='" + template.Name.ToLower() + "' class='template-heading'>";
                    html += string.Format(
                        "<a class='browse-expand' onclick='getFields($(this))'>+</a><span>{0}</span><a class='select-all' href='javascript:void(0)' onclick='selectAllFields($(this))'>select all</a>",
                        template.Name);
                    html += "<ul class='field-list'>";

                    html += "</ul>";
                    html += "</li>";
                }
            }

            html += "</ul>";

            return html;
        }

        #endregion

        #region Run Export

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

                var dateVal = new DateTime();
                var includeDateCreated = chkDateCreated.Checked || (!String.IsNullOrEmpty(txtStartDateCr.Value) && DateTime.TryParse(txtStartDateCr.Value, out dateVal)) || (!String.IsNullOrEmpty(txtEndDateCr.Value) && DateTime.TryParse(txtEndDateCr.Value, out dateVal));
                var includeCreatedBy = chkCreatedBy.Checked;
                var includeDateModified = chkDateModified.Checked || (!String.IsNullOrEmpty(txtStartDatePb.Value) && DateTime.TryParse(txtStartDatePb.Value, out dateVal)) || (!String.IsNullOrEmpty(txtEndDatePu.Value) && DateTime.TryParse(txtEndDatePu.Value, out dateVal)); ;
                var includeModifiedBy = chkModifiedBy.Checked;
                var neverPublish = chkNeverPublish.Checked;
                var includeReferrers = chkReferrers.Checked;

                var allLanguages = chkAllLanguages.Checked;
                var selectedLanguage = ddLanguages.SelectedValue;

                var templateString = inputTemplates.Value;
                var templates = templateString.ToLower().Split(',').Select(x => x.Trim()).ToList();

                if (chkIncludeInheritance.Checked && !String.IsNullOrEmpty(templateString))
                {                    
                    templates.AddRange(GetInheritors(templates));
                }
                                  
                List<Item> items = GetItems();

                _fieldsList = new List<FieldData>();
                var fields = fieldString.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                if (chkAllFields.Checked)
                {
                    fields = new List<string>();
                }

                StartResponse(!string.IsNullOrWhiteSpace(txtFileName.Value) ? txtFileName.Value : "ContentExport");

                using (StringWriter sw = new StringWriter())
                {
                    var headingString = "Item Path,"
                                        + (includeName ? "Name," : string.Empty)
                                        + (includeIds ? "Item ID," : string.Empty)
                                        + (includeTemplate ? "Template," : string.Empty)
                                        +
                                        (allLanguages || !string.IsNullOrWhiteSpace(selectedLanguage)
                                            ? "Language,"
                                            : string.Empty)
                                        + (includeDateCreated ? "Created," : string.Empty)
                                        + (includeCreatedBy ? "Created By," : string.Empty)
                                        + (includeDateModified ? "Modified," : string.Empty)
                                        + (includeModifiedBy ? "Modified By," : string.Empty)
                                        + (neverPublish ? "Never Publish," : string.Empty)
                                        + (includeworkflowName ? "Workflow," : string.Empty)
                                        + (includeWorkflowState ? "Workflow State," : string.Empty)
                                        + (includeReferrers ? "Referrers," : string.Empty);

                    var dataLines = new List<string>();

                    foreach (var baseItem in items)
                    {
                        var itemVersions = GetItemVersions(baseItem, allLanguages, selectedLanguage);

                        foreach (var item in itemVersions)
                        {
                            var itemPath = item.Paths.ContentPath;
                            if (String.IsNullOrEmpty(itemPath)) continue;
                            var itemLine = itemPath + ",";

                            if (includeName)
                            {
                                itemLine += item.Name + ",";
                            }                     

                            if (includeIds)
                            {
                                itemLine += item.ID + ",";
                            }

                            if (includeTemplate)
                            {
                                var template = item.TemplateName;
                                itemLine += template + ",";
                            }

                            if (allLanguages || !string.IsNullOrWhiteSpace(selectedLanguage))
                            {
                                itemLine += item.Language.GetDisplayName() + ",";
                            }

                            if (includeDateCreated)
                            {
                                itemLine += item.Statistics.Created.ToString("d") + ",";
                            }
                            if (includeCreatedBy)
                            {
                                itemLine += item.Statistics.CreatedBy + ",";
                            }
                            if (includeDateModified)
                            {
                                itemLine += item.Statistics.Updated.ToString("d") + ",";
                            }
                            if (includeModifiedBy)
                            {
                                itemLine += item.Statistics.UpdatedBy + ",";
                            }
                            if (neverPublish)
                            {
                                var neverPublishVal = item.Publishing.NeverPublish;
                                itemLine += neverPublishVal.ToString() + ",";
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
                                itemLine = AddWorkFlow(item, itemLine, includeworkflowName, includeWorkflowState);
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
                                            data += ";";
                                        }
                                        data += referrer.Paths.ContentPath;
                                        first = false;
                                    }
                                }
                                itemLine += "\"" + data + "\",";

                            }

                            foreach (var field in fields)
                            {
                                var itemLineAndHeading = AddFieldsToItemLineAndHeading(item, field, itemLine,
                                    headingString, includeLinkedIds, includeRawHtml);
                                itemLine = itemLineAndHeading.Item1;
                                headingString = itemLineAndHeading.Item2;
                            }                            

                            dataLines.Add(itemLine);
                        }
                    }

                    headingString += GetExcelHeaderForFields(_fieldsList, includeLinkedIds, includeRawHtml);


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
                            newLine = newLine.Replace(String.Format("{0}-ID", fieldName), headingString.Contains(String.Format("{0} ID", fieldName)) ? "n/a," : string.Empty);
                            newLine = newLine.Replace(String.Format("{0}-HTML", fieldName), headingString.Contains(String.Format("{0} Raw HTML", fieldName)) ? "n/a," : string.Empty);
                        }
                        sw.WriteLine(newLine);
                    }

                    SetCookieAndResponse(sw.ToString());               
                }
            }
            catch (Exception ex)
            {
                litFeedback.Text = "<span style='color:red'>" + ex + "</span>";
            }
        }

        private List<Item> GetItemVersions(Item item, bool allLanguages, string selectedLanguage)
        {
            var itemVersions = new List<Item>();
            if (allLanguages)
            {
                foreach (var language in item.Languages)
                {
                    var languageItem = item.Database.GetItem(item.ID, language);
                    if (languageItem.Versions.Count > 0)
                    {
                        itemVersions.Add(languageItem);
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(selectedLanguage))
            {
                foreach (var language in item.Languages)
                {
                    if (language.GetDisplayName() == selectedLanguage)
                    {
                        var languageItem = item.Database.GetItem(item.ID, language);
                        if (languageItem.Versions.Count > 0)
                        {
                            itemVersions.Add(languageItem);
                        }
                    }
                }
            }
            else
            {
                itemVersions.Add(item);
            }
            return itemVersions;
        }

        private string AddWorkFlow(Item item, string itemLine, bool includeworkflowName, bool includeWorkflowState)
        {
            var workflowProvider = item.Database.WorkflowProvider;
            if (workflowProvider == null)
            {
                if (includeworkflowName && includeWorkflowState)
                {
                    itemLine += ",";
                }
                itemLine += ",";
            }
            else
            {
                var workflow = workflowProvider.GetWorkflow(item);
                if (workflow == null)
                {
                    if (includeworkflowName && includeWorkflowState)
                    {
                        itemLine += ",";
                    }
                    itemLine += ",";
                }
                else
                {
                    if (includeworkflowName)
                    {
                        itemLine += workflow + ",";
                    }
                    if (includeWorkflowState)
                    {
                        var workflowState = workflow.GetState(item);
                        itemLine += workflowState.DisplayName + ",";
                    }
                }
            }
            return itemLine;
        }

        private Tuple<string, string> AddFieldsToItemLineAndHeading(Item item, string field, string itemLine, string headingString, bool includeLinkedIds, bool includeRawHtml)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                var fieldName = GetFieldNameIfGuid(field);
                var itemField = item.Fields[field];
                bool rawField = false;
                bool idField = false;
                if (itemField == null)
                {
                    if (_fieldsList.All(x => x.fieldName != field))
                    {
                        _fieldsList.Add(new FieldData()
                        {
                            field = null,
                            fieldName = fieldName,
                            fieldType = null,
                            rawHtml = false,
                            linkedId = false
                        });
                    }
                    itemLine += String.Format("n/a,{0}-ID{0}-HTML", fieldName);
                }
                else
                {
                    Tuple<string, string> lineAndHeading = null;
                    var itemOfType = FieldTypeManager.GetField(itemField);
                    if (itemOfType is ImageField) // if image field
                    {
                        lineAndHeading = ParseImageField(itemField, itemLine, headingString, fieldName,
                            includeLinkedIds, includeRawHtml);
                        rawField = true;
                        idField = true;
                    }
                    else if (itemOfType is LinkField)
                    {
                        lineAndHeading = ParseLinkField(itemField, itemLine, headingString, fieldName,
                            includeLinkedIds, includeRawHtml);
                        rawField = true;
                    }
                    else if (itemOfType is ReferenceField || itemOfType is GroupedDroplistField || itemOfType is LookupField)
                    {
                        lineAndHeading = ParseReferenceField(itemField, itemLine, headingString, fieldName,
                            includeLinkedIds, includeRawHtml);
                        idField = true;
                    }
                    else if (itemOfType is MultilistField)
                    {
                        lineAndHeading = ParseMultilistField(itemField, itemLine, headingString, fieldName,
                            includeLinkedIds, includeRawHtml);
                        idField = true;
                    }
                    else if (itemOfType is CheckboxField)
                    {
                        lineAndHeading = ParseCheckboxField(itemField, itemLine, headingString, fieldName);
                    }
                    else if (itemOfType is DateField)
                    {
                        lineAndHeading = ParseDateField(itemField, itemLine, headingString);
                    }
                    else // default text field
                    {
                        lineAndHeading = ParseDefaultField(itemField, itemLine, headingString, fieldName);
                    }

                    if (_fieldsList.All(x => x.fieldName != fieldName))
                    {
                        _fieldsList.Add(new FieldData()
                        {
                            field = itemField,
                            fieldName = fieldName,
                            fieldType = itemField.Type,
                            rawHtml = rawField,
                            linkedId = idField
                        });
                    }
                    else
                    {
                        // check for nulls
                        var fieldItem = _fieldsList.FirstOrDefault(x => x.fieldName == fieldName && x.field == null);
                        if (fieldItem != null)
                        {
                            fieldItem.field = itemField;
                            fieldItem.fieldType = itemField.Type;
                            fieldItem.rawHtml = rawField;
                            fieldItem.linkedId = idField;
                        }
                    }

                    itemLine = lineAndHeading.Item1;
                    headingString = lineAndHeading.Item2;
                }
            }

            return new Tuple<string, string>(itemLine, headingString);
        }

        #region FieldParsingMethods

        private Tuple<string, string> ParseImageField(Field itemField, string itemLine, string headingString, string fieldName, bool includeLinkedIds, bool includeRawHtml)
        {
            ImageField imageField = itemField;
            if (includeLinkedIds)
            {
                headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Format("{0} ID,", fieldName));
            }
            if (includeRawHtml)
            {
                headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Format("{0} Raw HTML,", fieldName));
            }
            if (imageField == null)
            {
                itemLine += "n/a,";

                if (includeLinkedIds)
                {
                    itemLine += "n/a,";
                }

                if (includeRawHtml)
                {
                    itemLine += "n/a,";
                }
            }
            else if (imageField.MediaItem == null)
            {

                itemLine += ",";
                if (includeLinkedIds)
                {
                    itemLine += ",";
                }

                if (includeRawHtml)
                {
                    itemLine += ",";
                }
            }
            else
            {
                itemLine += imageField.MediaItem.Paths.MediaPath + ",";
                if (includeLinkedIds)
                {
                    itemLine += imageField.MediaItem.ID + ",";
                }

                if (includeRawHtml)
                {
                    itemLine += imageField.Value + ",";
                }
            }
            return new Tuple<string, string>(itemLine, headingString);
        }

        private Tuple<string, string> ParseLinkField(Field itemField, string itemLine, string headingString, string fieldName, bool includeLinkedIds, bool includeRawHtml)
        {
            LinkField linkField = itemField;
            if (includeLinkedIds)
            {
                headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Empty);
            }
            if (includeRawHtml)
            {
                headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Format("{0} Raw HTML,", fieldName));
            }
            if (linkField == null)
            {
                itemLine += "n/a,";

                if (includeRawHtml)
                {
                    itemLine += "n/a,";
                }
            }
            else
            {
                itemLine += linkField.Url + ",";

                if (includeRawHtml)
                {
                    itemLine += linkField.Value + ",";
                }
            }
            return new Tuple<string, string>(itemLine, headingString);
        }

        private Tuple<string, string> ParseReferenceField(Field itemField, string itemLine, string headingString, string fieldName, bool includeLinkedIds, bool includeRawHtml)
        {
            ReferenceField refField = itemField;
            if (includeLinkedIds)
            {
                headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Format("{0} ID,", fieldName));
            }
            if (includeRawHtml)
            {
                headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Empty);
            }
            if (refField == null)
            {
                itemLine += "n/a,";
                if (includeLinkedIds)
                {
                    itemLine += "n/a,";
                }
            }
            else if (refField.TargetItem == null)
            {
                itemLine += ",";
                if (includeLinkedIds)
                {
                    itemLine += ",";
                }
            }
            else
            {
                itemLine += refField.TargetItem.Paths.ContentPath + ",";
                if (includeLinkedIds)
                {
                    itemLine += refField.TargetID + ",";
                }
            }
            return new Tuple<string, string>(itemLine, headingString);
        }

        private Tuple<string, string> ParseMultilistField(Field itemField, string itemLine, string headingString, string fieldName, bool includeLinkedIds, bool includeRawHtml)
        {
            MultilistField multiField = itemField;
            if (includeLinkedIds)
            {
                headingString = headingString.Replace(String.Format("{0}-ID", fieldName), String.Format("{0} ID,", fieldName));
            }
            if (includeRawHtml)
            {
                headingString = headingString.Replace(String.Format("{0}-HTML", fieldName), String.Empty);
            }
            if (multiField == null)
            {
                itemLine += "n/a,";
                if (includeLinkedIds)
                {
                    itemLine += "n/a,";
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
                        data += ";";
                    }
                    var url = i.Paths.ContentPath;
                    data += url;
                    first = false;
                }
                itemLine += "\"" + data + "\"" + ",";

                if (includeLinkedIds)
                {
                    first = true;
                    var idData = "";
                    foreach (var i in multiItems)
                    {
                        if (!first)
                        {
                            idData += ";";
                        }
                        idData += i.ID + ";";
                        first = false;
                    }
                    itemLine += "\"" + idData + "\"" + ",";
                }
            }
            return new Tuple<string, string>(itemLine, headingString);
        }

        private Tuple<string, string> ParseCheckboxField(Field itemField, string itemLine, string headingString, string fieldName)
        {
            CheckboxField checkboxField = itemField;
            headingString = headingString.Replace(String.Format("{0}-ID", fieldName), string.Empty).Replace(String.Format("{0}-HTML", fieldName), string.Empty);
            itemLine += checkboxField.Checked.ToString() + ",";
            return new Tuple<string, string>(itemLine, headingString);
        }

        private Tuple<string, string> ParseDateField(Field itemField, string itemLine, string headingString)
        {
            DateField dateField = itemField;

            itemLine += dateField.DateTime.ToString("d");

            itemLine += ",";
            return new Tuple<string, string>(itemLine, headingString);
        }

        private Tuple<string, string> ParseDefaultField(Field itemField, string itemLine, string headingString, string fieldName)
        {
            var fieldValue = RemoveLineEndings(itemField.Value);
            if (fieldValue.Contains("\""))
            {
                fieldValue = fieldValue.Replace("\"", "\"\"");
            }
            if (fieldValue.Contains(","))
            {
                fieldValue = "\"" + fieldValue + "\"";
            }
            itemLine += fieldValue + ",";
            headingString = headingString.Replace(String.Format("{0}-ID", fieldName), string.Empty).Replace(String.Format("{0}-HTML", fieldName), string.Empty);
            return new Tuple<string, string>(itemLine, headingString);
        }

        #endregion

        private List<String> GetInheritors(List<string> templates)
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
                            template.Replace("{", string.Empty).Replace("}", string.Empty) ||
                            x.Paths.FullPath.ToLower() == template.ToLower());

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
            return inheritors;
        }

        public string GetExcelHeaderForFields(IEnumerable<FieldData> fields, bool includeId, bool includeRaw)
        {
            var header = "";
            foreach (var field in fields)
            {
                var fieldName = field.fieldName;

                header += fieldName + ",";

                if (includeId && field.linkedId)
                {
                    header += String.Format("{0} ID", fieldName) + ",";
                }

                if (includeRaw && field.rawHtml)
                {
                    header += String.Format("{0} Raw HTML", fieldName) + ",";
                }
            }
            return header;
        }



        public string RemoveLineEndings(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            return value.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(lineSeparator, string.Empty).Replace(paragraphSeparator, string.Empty).Replace("<br/>", string.Empty).Replace("<br />", string.Empty).Replace("\t", "   ");
        }

        #endregion

        #region Run Export
        protected void btnCreateItems_OnClick(object sender, EventArgs e)
        {
            ProcessImport(true);
        }

        protected void btnEditItems_OnClick(object sender, EventArgs e)
        {
            ProcessImport(false);
        }

        protected void ProcessImport(bool createItems)
        {
            try
            {
                var output = "";
                _db = Sitecore.Configuration.Factory.GetDatabase("master");
                var file = btnFileUpload.PostedFile;
                if (file == null)
                {
                    litUploadResponse.Text = "You must select a file first<br/>";
                }

                var fieldsMap = new List<String>();
                var itemPathIndex = 0;
                var itemNameIndex = 0;
                var itemTemplateIndex = 0;
                var itemsImported = 0;

                using (TextReader tr = new StreamReader(file.InputStream))
                {
                    CsvParser csv = new CsvParser(tr);
                    List<string[]> rows = csv.GetRows();
                    for (var i = 0; i < rows.Count; i++)
                    {
                        var line = i;
                        var cells = rows[i];
                        if (i == 0)
                        {
                            // create fields map
                            fieldsMap = cells.ToList();
                            itemPathIndex = fieldsMap.FindIndex(x => x.ToLower() == "item path");
                            itemTemplateIndex = fieldsMap.FindIndex(x => x.ToLower() == "template");
                            itemNameIndex = fieldsMap.FindIndex(x => x.ToLower() == "name");
                        }
                        else
                        {
                            var path = cells[itemPathIndex];
                            Guid guid;
                            if (!Guid.TryParse(path, out guid) && !path.ToLower().StartsWith("/sitecore/content"))
                            {
                                path = "/sitecore/content" + (path.StartsWith("/") ? "" : "/") + path;
                            }

                            // if we are editing items, then the current item = Item Path; if we are creatign items, then are our item is created under Item Path item
                            Item item = _db.GetItem(path);
                            if (item == null)
                            {
                                output += "Line " + (line + 1) + " skipped; could not find " + path + "<br/>";
                                continue;
                            }
                            if (createItems)
                            {
                                if (itemNameIndex == -1 || itemTemplateIndex == -1)
                                {
                                    output += "Name and Template columns are required to create items<br/>";
                                    litUploadResponse.Text = output;
                                    return;
                                }
                                var name = cells[itemNameIndex];
                                var template = cells[itemTemplateIndex];
                                if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(template))
                                {
                                    output += "Line " + (line + 1) + " skipped; name or template not specified<br/>";
                                    continue;
                                }
                                var templateItem = _db.GetTemplate(template);
                                if (templateItem == null)
                                {
                                    output += "Line " + (line + 1) + " skipped; could not find template<br/>";
                                    continue;
                                }
                                try
                                {
                                    if (chkNoDuplicates.Checked)
                                    {
                                        var newItemPath = item.Paths.FullPath + "/" + name;
                                        var existingItem = _db.GetItem(newItemPath);
                                        if (existingItem != null && existingItem.TemplateID == templateItem.ID)
                                        {
                                            output += "Line " + (line + 1) + " skipped; item with that name already exists at that location<br/>";
                                            continue;
                                        }
                                    }

                                    var newItem = item.Add(name, templateItem);
                                    item = newItem;
                                    if (item == null)
                                    {
                                        output += "Line " + (line + 1) + " skipped; could not create item<br/>";
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    output += "Line " + (line + 1) + " skipped; could not create item - invalid item name<br/>";
                                    continue;
                                }
                            }
                            EditItem(item, cells, itemPathIndex, itemNameIndex, itemTemplateIndex, fieldsMap, i, ref output);
                            itemsImported++;
                        }
                    }
                }
                if (itemsImported > 0)
                {
                    output = "Successfully " + (createItems ? "created " : "edited ") + itemsImported + " items<br/>" +
                             output;
                }
                else
                {
                    output = "No items were imported<br/>" + output;
                }
                btnFileUpload.Dispose();


                litUploadResponse.Text = output;
            }
            catch (Exception ex)
            {
                litUploadResponse.Text = "Oops! An error occurred while importing: <br/>" + ex;
            }
        }

        protected void EditItem(Item item, string[] cells, int itemPathIndex, int itemNameIndex, int itemTemplateIndex, List<string> fieldsMap, int line, ref string output)
        {
            item.Editing.BeginEdit();
            for (var i = 0; i < cells.Length; i++)
            {
                // skip path, name, and template fields
                if (i == itemPathIndex || i == itemNameIndex || i == itemTemplateIndex) continue;
                if (i > fieldsMap.Count - 1)
                {
                    output += "Line " + (line + 1) + ": Column " + i + " ('" + cells[i] +  "') not used, no corresponding Field in Header<br/>";
                }
                var fieldName = fieldsMap[i];
                var value = cells[i];

                try
                {
                    if (String.IsNullOrEmpty(fieldName)) continue;
                    Field itemField = item.Fields[fieldName];
                    if (itemField == null)
                    {
                        output += "Unable to set " + fieldName + ", line " + i + 1 + ": Field not found" + "<br/>";
                        continue;
                    }

                    var itemOfType = FieldTypeManager.GetField(itemField);
                    if (itemOfType is ImageField) // if image field
                    {
                        var imageField = (ImageField)item.Fields[fieldName];
                        var mediaPath = value;
                        Guid id;
                        if (!Guid.TryParse(value, out id))
                        {
                            if (!mediaPath.ToLower().StartsWith("/sitecore/media library"))
                            {
                                mediaPath = "/sitecore/media library" + (mediaPath.StartsWith("/") ? "" : "/") + value;
                            }
                        }
                        MediaItem mediaItem = _db.GetItem(mediaPath);
                        if (mediaItem == null)
                        {
                            output += "Line " + (line + 1) + ": Unable to set " + fieldName + ": Could not find image" + "<br/>";
                            continue;
                        }
                        imageField.MediaID = mediaItem.ID;
                    }
                    else if (itemOfType is LinkField)
                    {
                        var linkField = (LinkField)item.Fields[fieldName];
                        linkField.Url = value;

                    }
                    else if (itemOfType is ReferenceField || itemOfType is GroupedDroplistField || itemOfType is LookupField)
                    {
                        var refItem = GetReferenceFieldItem(value, itemField);
                        if (refItem != null)
                        {
                            item[fieldName] = refItem.ID.Guid.ToString();
                        }
                    }
                    else if (itemOfType is MultilistField)
                    {
                        MultilistField multiField = (MultilistField) item.Fields[fieldName];
                        var values = value.Split(';').Where(x => !String.IsNullOrEmpty(x));
                        List<Item> refItems = new ItemList();

                        foreach (var val in values)
                        {
                            var refItem = GetReferenceFieldItem(val, itemField);
                            if (refItem == null)
                            {
                                output += "Line " + (line + 1) + ": Unable to find " + fieldName + " item " + val + "<br/>";
                            }
                            else
                            {
                                refItems.Add(refItem);
                            }
                        }
                        var ids = String.Join("|", refItems.Select(x => x.ID));
                        item[fieldName] = ids;
                    }
                    else if (itemOfType is CheckboxField)
                    {
                        bool boolean;
                        if (Boolean.TryParse(value, out boolean))
                        {
                            var checkboxField = (CheckboxField)item.Fields[fieldName];
                            checkboxField.Checked = boolean;
                        }
                    }
                    else if (itemOfType is DateField)
                    {
                        DateTime date;
                        if (DateTime.TryParse(value, out date))
                        {
                            var isoDate = DateUtil.ToIsoDate(date);
                            item[fieldName] = isoDate;
                        }

                    }
                    else // default text field
                    {
                        item[fieldName] = value;
                    }
                }
                catch (Exception ex)
                {
                    output += "Line " + (line + 1) + ": Unable to set " + fieldName + ": " + ex.Message + "<br/>";
                }
            }
            item.Editing.EndEdit();
        }

        protected Item GetReferenceFieldItem(string value, Field itemField)
        {
            Guid id;
            if (Guid.TryParse(value, out id))
            {
                return _db.GetItem(id.ToString());
            }
            else
            {
                // see if value is a path
                var path = value;
                if (!value.ToLower().StartsWith("/sitecore/content"))
                {
                    path = "/sitecore/content" + value;
                }
                var itemFromPath = _db.GetItem(path);
                if (itemFromPath != null) return itemFromPath;

                var fieldId = itemField.ID;
                var fieldItem = _db.GetItem(fieldId.ToString());
                var sourceField = (TemplateFieldSourceField)fieldItem.Fields["Source"];
                if (sourceField != null)
                {
                    // try get path
                    Item[] items;
                    var rootItem = _db.GetItem(sourceField.Path);
                    if (rootItem != null)
                    {
                        items = rootItem.Axes.GetDescendants();
                    }
                    else
                    {
                        items = _db.SelectItems(sourceField.Path);
                    }
                    if (items != null)
                    {
                        var refItem = items.FirstOrDefault(x => x.DisplayName.ToLower() == value.ToLower());
                        return refItem;
                    }
                }
                return null;
            }
        }

        protected void btnDownloadCSVTemplate_OnClick(object sender, EventArgs e)
        {
            StartResponse("CSVImportTemplate");
            using (StringWriter sw = new StringWriter())
            {
                var headingString = "Item Path,Template,Name,Field1,Field2,Field3";
                sw.WriteLine(headingString);
                SetCookieAndResponse(sw.ToString());
            }
        }
        #endregion

        #region Test Fast Query

        protected void btnTestFastQuery_OnClick(object sender, EventArgs e)
        {
            HideModals(false, false, false);
            if (!SetDatabase()) SetDatabase("web");

            var fastQuery = txtFastQuery.Value;
            if (string.IsNullOrWhiteSpace(fastQuery)) return;

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

        #endregion

        #region Save Settings

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
                AllFields = chkAllFields.Checked,
                AdvancedSearch = txtAdvancedSearch.Value,
                StartDateCr = txtStartDateCr.Value,
                EndDateCr = txtEndDateCr.Value,
                StartDatePb = txtStartDatePb.Value,
                EndDatePb = txtEndDatePu.Value,
                DateRangeAnd = radDateRangeAnd.Checked
            };

            var settingsObject = new ExportSettings()
            {
                Name = saveName,
                Data = settingsData,
                UserId = GetUserId(),
                ID = Guid.NewGuid().ToString()
            };

            var serializer = new JavaScriptSerializer();

            var savedSettings = ReadSettingsFromFile(true);

            if (savedSettings == null || savedSettings.Settings == null)
            {
                var settingsList = new SettingsList();
                settingsList.Settings = new List<ExportSettings>()
                {
                    settingsObject
                };
                var settingsJson = serializer.Serialize(settingsList);

                EditSavedSettingsItem(settingsJson);
            }
            else
            {
                if (savedSettings.Settings.Any(x => x.Name == saveName && x.UserId == GetUserId()))
                {
                    phOverwriteScript.Visible = true;
                    return;
                }

                savedSettings.Settings.Insert(0, settingsObject);
                var settingsListJson = serializer.Serialize(savedSettings);
                EditSavedSettingsItem(settingsListJson);
            }
            litSavedMessage.Text = "Saved!";
            SetSavedSettingsDropdown();
            ddSavedSettings.SelectedValue = settingsObject.ID;
        }

        protected void EditSavedSettingsItem(string settingsJson)
        {
            var settingsItem = Sitecore.Configuration.Factory.GetDatabase("master").GetItem(_settingsItemPath);
            settingsItem.Editing.BeginEdit();
            settingsItem["Settings"] = settingsJson;
            settingsItem.Editing.EndEdit();
        }

        protected ExportSettings GetSettingsFromFile(SettingsList savedSettings, string settingsId)
        {
            var selectedSettings = savedSettings.Settings.FirstOrDefault(x => x.ID == settingsId);
            if (selectedSettings == null)
            {
                var settingName = settingsId;
                if (settingName.Contains("["))
                {
                    settingName = settingName.Substring(0, settingName.IndexOf("[")).Trim();
                }
                selectedSettings = savedSettings.Settings.FirstOrDefault(x => x.Name == settingName);
            }
            return selectedSettings;
        }

        protected void ddSavedSettings_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            PhBrowseTree.Visible = false;
            PhBrowseTemplates.Visible = false;
            PhBrowseFields.Visible = false;

            var settingsId = ddSavedSettings.SelectedValue;
            if (string.IsNullOrWhiteSpace(settingsId))
            {
                btnDeletePrompt.Visible = false;
                ClearAll();
                return;
            }
            btnDeletePrompt.Visible = true;
            var savedSettings = ReadSettingsFromFile(chkAllUserSettings.Checked);
            if (savedSettings == null) return;

            var selectedSettings = GetSettingsFromFile(savedSettings, settingsId);

            var settings = selectedSettings.Data;

            if (!string.IsNullOrWhiteSpace(settings.Database))
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
            txtAdvancedSearch.Value = settings.AdvancedSearch;

            txtStartDateCr.Value = settings.StartDateCr;
            txtEndDateCr.Value = settings.EndDateCr;
            txtStartDatePb.Value = settings.StartDatePb;
            txtEndDatePu.Value = settings.EndDatePb;

            if (settings.DateRangeAnd)
            {
                radDateRangeOr.Checked = false;
                radDateRangeAnd.Checked = true;
            }
            else
            {
                radDateRangeOr.Checked = true;
                radDateRangeAnd.Checked = false;
            }

            if (SavedSettingsOpenAdvanced(settings))
            {
                divAdvOptions.Attributes["class"] = "advanced open open-default";
            }
        }

        public bool SavedSettingsOpenAdvanced(ExportSettingsData settings)
        {
            return (
                !String.IsNullOrEmpty(settings.AdvancedSearch) ||
                !String.IsNullOrEmpty(settings.StartDateCr) ||
                !String.IsNullOrEmpty(settings.EndDateCr) ||
                !String.IsNullOrEmpty(settings.StartDatePb) ||
                !String.IsNullOrEmpty(settings.EndDatePb) ||
                !String.IsNullOrEmpty(settings.MultipleStartPaths) ||
                settings.RequireLayout ||
                settings.IncludeLinkedIds ||
                settings.IncludeRaw ||
                settings.Referrers ||
                settings.DateCreated ||
                settings.DateModified ||
                settings.CreatedBy ||
                settings.ModifiedBy ||
                settings.NeverPublish ||
                settings.Workflow ||
                settings.WorkflowState ||
                !String.IsNullOrEmpty(settings.SelectedLanguage) ||
                settings.GetAllLanguages ||
                !String.IsNullOrEmpty(settings.FileName)
                );
        }

        #endregion

        #region Clear All

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
            txtAdvancedSearch.Value = string.Empty;
            txtStartDatePb.Value = string.Empty;
            txtEndDatePu.Value = string.Empty;
            txtStartDateCr.Value = string.Empty;
            txtEndDateCr.Value = string.Empty;
            chkNeverPublish.Checked = false;
            radDateRangeOr.Checked = true;
            radDateRangeAnd.Checked = false;

            PhBrowseTree.Visible = false;
            PhBrowseTemplates.Visible = false;
            PhBrowseFields.Visible = false;
        }

        #endregion

        #region Overwrite Settings

        protected void btnOverWriteSettings_OnClick(object sender, EventArgs e)
        {
            var settingsId = txtSaveSettingsName.Value;

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

            var savedSettings = ReadSettingsFromFile(false);

            var setting = GetSettingsFromFile(savedSettings, settingsId);

            if (setting == null) return;
            setting.Data = settingsData;
            var settingsListJson = serializer.Serialize(savedSettings);

            EditSavedSettingsItem(settingsListJson);

            litSavedMessage.Text = "Saved!";
            SetSavedSettingsDropdown(chkAllUserSettings.Checked);
            ddSavedSettings.SelectedValue = setting.ID;
        }

        #endregion

        #region Delete Saved Settings

        protected void btnDeleteSavedSetting_OnClick(object sender, EventArgs e)
        {
            PhBrowseTree.Visible = false;
            PhBrowseTemplates.Visible = false;
            PhBrowseFields.Visible = false;

            var settingsId = ddSavedSettings.SelectedValue;
            var savedSettings = ReadSettingsFromFile(true);

            var setting = GetSettingsFromFile(savedSettings, settingsId);
            var serializer = new JavaScriptSerializer();
            if (setting != null && setting.UserId == GetUserId())
            {
                savedSettings.Settings.Remove(setting);
                var settingsListJson = serializer.Serialize(savedSettings);
                EditSavedSettingsItem(settingsListJson);
                SetSavedSettingsDropdown();
            }else if (setting != null && setting.UserId != GetUserId())
            {
                phDeleteScript.Visible = true;
            }
        }

        #endregion

        #region Advanced Search

        protected void btnAdvancedSearch_OnClick(object sender, EventArgs e)
        {
            HideModals(false, false, false);
            if (!SetDatabase()) SetDatabase("web");

            var searchText = txtAdvancedSearch.Value;

            StartResponse(!string.IsNullOrWhiteSpace(txtFileName.Value) ? txtFileName.Value : "ContentSearch - " + searchText);

            var fieldString = inputFields.Value;
            var fields = fieldString.Split(',').Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x)).ToList();
            var items = GetItems();

            using (StringWriter sw = new StringWriter())
            {
                var headingString = "Item Path,Field";
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
                        var fieldsWithText = CheckAllFields(version, searchText, fields);
                        if (!string.IsNullOrWhiteSpace(fieldsWithText))
                        {
                            var dataLine = baseItem.Paths.ContentPath + "," + fieldsWithText;
                            if (version.Language.Name != LanguageManager.DefaultLanguage.Name)
                            {
                                dataLine += "," + version.Language.GetDisplayName();
                                if (!addedLangToHeading)
                                {
                                    addedLangToHeading = true;
                                    headingString += ",Language";
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

                SetCookieAndResponse(sw.ToString());
            }
        }

        protected string CheckAllFields(Item dataItem, string searchText, List<string> fields)
        {
            var fieldsSelected = fields.Any(x => !string.IsNullOrEmpty(x));
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
                if (fieldsSelected && fields.All(x => x != field.Name))
                {
                    continue; 
                }

                if (field == null || field.Name.StartsWith("__"))
                {
                    continue;
                }

                //Only add text based fields.
                if (FieldTypeManager.GetField(field) is HtmlField)
                {
                    var html = field.Value.ToLower();
                    if (html.Contains(searchText))
                    {
                        if (!string.IsNullOrWhiteSpace(fieldsWithText)) fieldsWithText += "; ";
                        fieldsWithText += field.Name;
                    }
                }

                //Add the field text to the overall searchable text.
                if (FieldTypeManager.GetField(field) is TextField)
                {
                    if (field.Value.ToLower().Contains(searchText))
                    {
                        if (!string.IsNullOrWhiteSpace(fieldsWithText)) fieldsWithText += "; ";
                        fieldsWithText += field.Name;
                    }
                }

                // droplist, treelist
                if (FieldTypeManager.GetField(field) is LookupField)
                {
                    var lookupField = (LookupField)field;
                    var tagName = GetTagName(lookupField.TargetItem);
                    if (!string.IsNullOrWhiteSpace(tagName) && tagName.ToLower().Contains(searchText))
                    {
                        if (!string.IsNullOrWhiteSpace(fieldsWithText)) fieldsWithText += "; ";
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
                        if (!string.IsNullOrWhiteSpace(tagName) && tagName.ToLower().Contains(searchText))
                        {
                            if (!string.IsNullOrWhiteSpace(fieldsWithText)) fieldsWithText += "; ";
                            fieldsWithText += field.Name;
                        }
                    }
                }

                else
                {
                    var ids = field.Value.Split('|').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
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
                                    if (!string.IsNullOrWhiteSpace(fieldsWithText)) fieldsWithText += "; ";
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
            if (item == null)
                return string.Empty;

            Field f = item.Fields["Title"];
            if (f == null)
                return string.Empty;

            return !string.IsNullOrWhiteSpace(f.Value)
                                ? f.Value
                                : item.Name;
        }

        #endregion

        #region Shared

        protected bool SetDatabase()
        {
            var databaseName = ddDatabase.SelectedValue;
            if (chkWorkflowName.Checked || chkWorkflowState.Checked)
            {
                databaseName = "master";
            }

            if (string.IsNullOrWhiteSpace(databaseName))
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

        public List<Item> GetItems()
        {
            var startNode = inputStartitem.Value;
            if (string.IsNullOrWhiteSpace(startNode)) startNode = "/sitecore/content";

            var templateString = inputTemplates.Value;
            var templates = templateString.ToLower().Split(',').Select(x => x.Trim()).ToList();
            var fastQuery = txtFastQuery.Value;

            var exportItems = new List<Item>();
            if (!string.IsNullOrWhiteSpace(fastQuery))
            {
                var queryItems = _db.SelectItems(fastQuery);
                exportItems = queryItems.ToList();
            }else if (!string.IsNullOrWhiteSpace(inputMultiStartItem.Value))
            {
                var startItems = inputMultiStartItem.Value.Split(',');
                foreach (var startItem in startItems)
                {
                    Item item = _db.GetItem(startItem.Trim());
                    if (item == null)
                        continue;

                    var descendants = item.Axes.GetDescendants();
                    exportItems.Add(item);
                    exportItems.AddRange(descendants);
                }
            }
            else
            {
                Item startItem = _db.GetItem(startNode);
                var descendants = startItem.Axes.GetDescendants();
                exportItems.Add(startItem);
                exportItems.AddRange(descendants);
            }
       
            // created AND published filters
            exportItems = FilterByDateRanges(exportItems);

            var items = new List<Item>();
            if (!string.IsNullOrWhiteSpace(templateString))
            {
                foreach (var template in templates)
                {
                    IEnumerable<Item> templateItems;

                    // try get template as guid or path
                    var templateItem = _db.GetItem(template);
                    if (templateItem != null)
                    {
                        templateItems = exportItems.Where(x => x.TemplateID == templateItem.ID);
                    }
                    else
                    {
                        templateItems = exportItems.Where(x => x.TemplateName.ToLower() == template || x.TemplateID.ToString().ToLower().Replace("{", string.Empty).Replace("}", string.Empty) == template.Replace("{", string.Empty).Replace("}", string.Empty));
                    }

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

            if (!chkAdvancedSelectionOff.Checked &&
                (!String.IsNullOrWhiteSpace(txtAdvFields.Value) || chkAdvAllLinkedItems.Checked))
            {
                items = GetLinkedItems(items);              
            }

            return items;
        }

        protected List<Item> GetLinkedItems(List<Item> items)
        {
            List<Item> linkedItems = new ItemList();
            foreach (var item in items)
            {
                var fields = new List<string>();
                if (chkAdvAllLinkedItems.Checked)
                {
                    // get linked items from all fields
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
                else
                {
                    fields = txtAdvFields.Value.ToLower().Split(',').Select(x => x.Trim()).ToList();
                }

                foreach (var field in fields)
                {
                    if (!string.IsNullOrWhiteSpace(field))
                    {
                        var itemField = item.Fields[field];
                        if (itemField != null)
                        {
                            var itemOfType = FieldTypeManager.GetField(itemField);
                            if (itemOfType is LinkField)
                            {
                                LinkField linkField = itemField;
                                var linkedItem = linkField.TargetItem;
                                if (linkedItem != null) linkedItems.Add(linkedItem);
                            }else if (itemOfType is ReferenceField)
                            {
                                ReferenceField refField = itemField;
                                var linkedItem = refField.TargetItem;
                                if (linkedItem != null) linkedItems.Add(linkedItem);
                            }else if (itemOfType is MultilistField)
                            {
                                MultilistField listField = itemField;
                                var listItems = listField.GetItems();
                                linkedItems.AddRange(listItems);
                            }
                        }
                    }
                }
            }
            return linkedItems;
        }

        protected List<Item> FilterByDateRanges(List<Item> exportItems)
        {
            var startDateCr = new DateTime();
            var startDatePb  = new DateTime();
            var endDateCr = new DateTime();
            var endDatePb = new DateTime();

            //start dates
            var validStartDateCr = !String.IsNullOrEmpty(txtStartDateCr.Value) &&
                                   DateTime.TryParse(txtStartDateCr.Value, out startDateCr);
            var validStartDatePb = !String.IsNullOrEmpty(txtStartDatePb.Value) &&
                                   DateTime.TryParse(txtStartDatePb.Value, out startDatePb);

            //end dates
            var validEndDateCr = !String.IsNullOrEmpty(txtEndDateCr.Value) &&
                                       DateTime.TryParse(txtEndDateCr.Value, out endDateCr);
            var validEndDatePb = !String.IsNullOrEmpty(txtEndDatePu.Value) &&
                                   DateTime.TryParse(txtEndDatePu.Value, out endDatePb);

            if (!validStartDateCr && !validStartDatePb && !validEndDateCr && !validEndDatePb) return exportItems;

            var createdFilterItems = new List<Item>();
            var updatedFilterItems = new List<Item>();

            if (validEndDateCr)
            {
                endDateCr = new DateTime(endDateCr.Year, endDateCr.Month, endDateCr.Day, 23, 59, 59);
            }
            if (validEndDatePb)
            {
                endDatePb = new DateTime(endDatePb.Year, endDatePb.Month, endDatePb.Day, 23, 59, 59);
            }

            // put in date values
            if (!validStartDateCr)
            {
                startDateCr = DateTime.MinValue;
            }
            if (!validEndDateCr)
            {
                endDateCr = DateTime.MaxValue;
            }
            if (!validStartDatePb)
            {
                startDatePb = DateTime.MinValue;
            }
            if (!validEndDatePb)
            {
                endDatePb = DateTime.MaxValue;
            }

            if (validStartDateCr || validEndDateCr || radDateRangeAnd.Checked) // only populate list if we've selected filters, otherwise should be empty
            {
                createdFilterItems =
                    exportItems.Where(
                        x =>
                            (x.Statistics.Created >= startDateCr && x.Statistics.Created <= endDateCr &&
                             x.Statistics.Created != DateTime.MinValue && x.Statistics.Created != DateTime.MaxValue))
                        .ToList();
            }

            if (validStartDatePb || validEndDatePb || radDateRangeAnd.Checked)
            {
                updatedFilterItems =
                    exportItems.Where(
                        x =>
                            (x.Statistics.Updated >= startDatePb && x.Statistics.Updated <= endDatePb &&
                             x.Statistics.Updated != DateTime.MinValue && x.Statistics.Updated != DateTime.MaxValue))
                        .ToList();
            }

            exportItems = radDateRangeOr.Checked ? createdFilterItems.Union(updatedFilterItems).ToList() : createdFilterItems.Intersect(updatedFilterItems).ToList();

            return exportItems.OrderByDescending(x => x.Paths.ContentPath).ToList();
        }

        public bool DoesItemHasPresentationDetails(Item item)
        {
            if (item != null)
            {
                return item.Fields[Sitecore.FieldIDs.LayoutField] != null
                       && !string.IsNullOrWhiteSpace(item.Fields[Sitecore.FieldIDs.LayoutField].Value);
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

        protected void HideModals(bool hideBrowse, bool hideTemplates, bool hideFields)
        {
            PhBrowseTree.Visible = hideBrowse;
            PhBrowseFields.Visible = hideTemplates;
            PhBrowseTemplates.Visible = hideFields;
        }

        protected SettingsList ReadSettingsFromFile(bool allUsers = false)
        {
            _db = Sitecore.Configuration.Factory.GetDatabase("master");
            var serializer = new JavaScriptSerializer();

            var settingsItem = _db.GetItem(_settingsItemPath);
            if (settingsItem == null)
            {
                var settingsFolder = _db.GetItem("/sitecore/system/Modules/Content Export Tool");
                settingsItem = settingsFolder.Add("Saved Settings", _db.GetTemplate(new ID("{6DB332FA-2FB1-4C7B-AEA5-CC6A8665BF77}")));
            }

            var fileContents = settingsItem.Fields["Settings"].Value;
            if (fileContents == null || String.IsNullOrEmpty(fileContents)) return new SettingsList();
            // convert into a list of settings
            var settingsList = serializer.Deserialize<SettingsList>(fileContents);

            // get settings that belong to current user
            var userId = GetUserId();
            if (userId != null && settingsList.Settings != null && !allUsers)
            {
                settingsList.Settings = settingsList.Settings.Where(x => string.IsNullOrWhiteSpace(x.UserId) || x.UserId == userId).ToList();
            }

            if (settingsList != null && settingsList.Settings != null)
            {
                foreach (var setting in settingsList.Settings)
                {
                    if (String.IsNullOrEmpty(setting.ID))
                        setting.ID = setting.Name;
                }
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

        protected void StartResponse(string fileName)
        {
            Response.Clear();
            Response.Buffer = true;
            Response.AddHeader("content-disposition", string.Format("attachment;filename={0}.csv", fileName));
            Response.Charset = "";
            Response.ContentType = "application/vnd.ms-excel";
        }

        protected void SetCookieAndResponse(string responseValue)
        {
            var downloadToken = txtDownloadToken.Value;
            var responseCookie = new HttpCookie("DownloadToken");
            responseCookie.Value = downloadToken;
			responseCookie.HttpOnly = false;
            responseCookie.Expires = DateTime.Now.AddDays(1);
            Response.Cookies.Add(responseCookie);
            Response.Output.Write(responseValue);
            Response.Flush();
            Response.End();
        }

        #endregion

        protected void chkAllUserSettings_OnCheckedChanged(object sender, EventArgs e)
        {
            var allUsers = chkAllUserSettings.Checked;
            SetSavedSettingsDropdown(allUsers);
        }
    }

    #region Classes

    public class MultipleBrowseItems
    {
        public BrowseItem Parent;
        public List<BrowseItem> Children;
    }

    public class BrowseItem
    {
        public string Id;
        public string Name;
        public string Path;
        public bool HasChildren;
        public string Template;
    }

    public class SitecoreItemApiResponse
    {
        public int statusCode { get; set; }
    }

    public class SettingsList
    {
        public List<ExportSettings> Settings;
    }

    public class ExportSettings
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public ExportSettingsData Data { get; set; }
        public string ID { get; set; }
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
        public string AdvancedSearch;
        public string StartDateCr;
        public string EndDateCr;
        public string StartDatePb;
        public string EndDatePb;
        public bool DateRangeAnd;
    }

    public class FieldData
    {
        public Field field;
        public string fieldName;
        public string fieldType;
        public bool rawHtml;
        public bool linkedId;
    }

    public class ItemLineData
    {
        public string itemLine;
        public string headerLine;
        public FieldData fieldData;
    }

    #endregion

    #region CsvParser

    public class CsvParser
    {
        private string delimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        private char escape = '"';
        private char quote = '"';
        private string quoteString = "\"";
        private string doubleQuoteString = "\"\"";
        private char[] quoteRequiredChars;
        private CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        private bool quoteAllFields;
        private bool quoteNoFields;
        private ReadingContext context;
        private IFieldReader fieldReader;
        private bool disposed;
        private int c = -1;

        public char[] InjectionCharacters
        {
            get { return new[] {'=', '@', '+', '-'}; } 
        }

        public char InjectionEscapeCharacter
        {
            get { return '\t'; }
        }

        /// <summary>
        /// Gets the <see cref="FieldReader"/>.
        /// </summary>
        public IFieldReader FieldReader {
			get{
				return fieldReader;
			}
		}

        /// <summary>
        /// Creates a new parser using the given <see cref="TextReader" />.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader" /> with the CSV file data.</param>
        public CsvParser(TextReader reader) : this(new CsvFieldReader(reader, new Configuration(), false)) { }

        /// <summary>
        /// Creates a new parser using the given <see cref="TextReader" />.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader" /> with the CSV file data.</param>
        /// <param name="leaveOpen">true to leave the reader open after the CsvReader object is disposed, otherwise false.</param>
        public CsvParser(TextReader reader, bool leaveOpen) : this(new CsvFieldReader(reader, new Configuration(), leaveOpen)) { }

        /// <summary>
        /// Creates a new parser using the given <see cref="TextReader"/> and <see cref="Configuration"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> with the CSV file data.</param>
        /// <param name="configuration">The configuration.</param>
        public CsvParser(TextReader reader, Configuration configuration) : this(new CsvFieldReader(reader, configuration, false)) { }

        /// <summary>
        /// Creates a new parser using the given <see cref="TextReader"/> and <see cref="Configuration"/>.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader"/> with the CSV file data.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="leaveOpen">true to leave the reader open after the CsvReader object is disposed, otherwise false.</param>
        public CsvParser(TextReader reader, Configuration configuration, bool leaveOpen) : this(new CsvFieldReader(reader, configuration, leaveOpen)) { }

        /// <summary>
        /// Creates a new parser using the given <see cref="FieldReader"/>.
        /// </summary>
        /// <param name="fieldReader">The field reader.</param>
        public CsvParser(IFieldReader fieldReader)
        {
            this.fieldReader = fieldReader;
            context = fieldReader.Context as ReadingContext;
        }

        public List<string[]> GetRows()
        {
            // Don't forget about the async method below!
            List<string[]> rows = new List<string[]>();
            do
            {
                context.Record = Read();
                if (context.Record != null) rows.Add(context.Record);
            }
            while (context.Record != null);

            context.CurrentIndex = -1;
            context.HasBeenRead = true;

            return rows;
        }

        public string[] Read()
        {
            try
            {
                var row = ReadLine();

                return row;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string[] ReadLine()
        {
            context.RecordBuilder.Clear();
            context.Row++;
            context.RawRow++;

            while (true)
            {
                if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                {
                    // End of file.
                    if (context.RecordBuilder.Length > 0)
                    {
                        // There was no line break at the end of the file.
                        // We need to return the last record first.
                        context.RecordBuilder.Add(fieldReader.GetField());
                        return context.RecordBuilder.ToArray();
                    }

                    return null;
                }

                c = fieldReader.GetChar();

                if (context.RecordBuilder.Length == 0 && ((c == context.ParserConfiguration.Comment && context.ParserConfiguration.AllowComments) || c == '\r' || c == '\n'))
                {
                    ReadBlankLine();
                    if (!context.ParserConfiguration.IgnoreBlankLines)
                    {
                        break;
                    }

                    continue;
                }

                // Trim start outside of quotes.
                if (c == ' ' && (context.ParserConfiguration.TrimOptions & TrimOptions.Trim) == TrimOptions.Trim)
                {
                    ReadSpaces();
                    fieldReader.SetFieldStart(-1);
                }

                if (c == context.ParserConfiguration.Quote && !context.ParserConfiguration.IgnoreQuotes)
                {
                    if (ReadQuotedField())
                    {
                        break;
                    }
                }
                else
                {
                    if (ReadField())
                    {
                        break;
                    }
                }
            }

            return context.RecordBuilder.ToArray();
        }

        protected virtual bool ReadQuotedField()
        {
            var inQuotes = true;
            var quoteCount = 1;
            // Set the start of the field to after the quote.
            fieldReader.SetFieldStart();

            while (true)
            {
                var cPrev = c;

                if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                {
                    // End of file.
                    fieldReader.SetFieldEnd();
                    context.RecordBuilder.Add(fieldReader.GetField());

                    return true;
                }

                c = fieldReader.GetChar();

                // Trim start inside quotes.
                if (quoteCount == 1 && c == ' ' && cPrev == context.ParserConfiguration.Quote && (context.ParserConfiguration.TrimOptions & TrimOptions.InsideQuotes) == TrimOptions.InsideQuotes)
                {
                    ReadSpaces();
                    cPrev = ' ';
                    fieldReader.SetFieldStart(-1);
                }

                // Trim end inside quotes.
                if (inQuotes && c == ' ' && (context.ParserConfiguration.TrimOptions & TrimOptions.InsideQuotes) == TrimOptions.InsideQuotes)
                {
                    fieldReader.SetFieldEnd(-1);
                    fieldReader.AppendField();
                    fieldReader.SetFieldStart(-1);
                    ReadSpaces();
                    cPrev = ' ';

                    if (c == context.ParserConfiguration.Escape || c == context.ParserConfiguration.Quote)
                    {
                        inQuotes = !inQuotes;
                        quoteCount++;

                        cPrev = c;

                        if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                        {
                            // End of file.
                            fieldReader.SetFieldStart();
                            fieldReader.SetFieldEnd();
                            context.RecordBuilder.Add(fieldReader.GetField());

                            return true;
                        }

                        c = fieldReader.GetChar();

                        if (c == context.ParserConfiguration.Quote)
                        {
                            // If we find a second quote, this isn't the end of the field.
                            // We need to keep the spaces in this case.

                            inQuotes = !inQuotes;
                            quoteCount++;

                            fieldReader.SetFieldEnd(-1);
                            fieldReader.AppendField();
                            fieldReader.SetFieldStart();

                            continue;
                        }
                        else
                        {
                            // If there isn't a second quote, this is the end of the field.
                            // We need to ignore the spaces.
                            fieldReader.SetFieldStart(-1);
                        }
                    }
                }

                if (inQuotes && c == context.ParserConfiguration.Escape || c == context.ParserConfiguration.Quote)
                {
                    inQuotes = !inQuotes;
                    quoteCount++;

                    if (!inQuotes)
                    {
                        // Add an offset for the quote.
                        fieldReader.SetFieldEnd(-1);
                        fieldReader.AppendField();
                        fieldReader.SetFieldStart();
                    }

                    continue;
                }

                if (inQuotes)
                {
                    if (c == '\r' || (c == '\n' && cPrev != '\r'))
                    {
                        if (context.ParserConfiguration.LineBreakInQuotedFieldIsBadData)
                        {
                            context.ParserConfiguration.BadDataFound.Invoke(context);
                        }

                        // Inside a quote \r\n is just another character to absorb.
                        context.RawRow++;
                    }
                }

                if (!inQuotes)
                {
                    // Trim end outside of quotes.
                    if (c == ' ' && (context.ParserConfiguration.TrimOptions & TrimOptions.Trim) == TrimOptions.Trim)
                    {
                        ReadSpaces();
                        fieldReader.SetFieldStart(-1);
                    }

                    if (c == context.ParserConfiguration.Delimiter[0])
                    {
                        fieldReader.SetFieldEnd(-1);

                        if (ReadDelimiter())
                        {
                            // Add an extra offset because of the end quote.
                            context.RecordBuilder.Add(fieldReader.GetField());

                            return false;
                        }
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        fieldReader.SetFieldEnd(-1);
                        var offset = ReadLineEnding();
                        fieldReader.SetRawRecordEnd(offset);
                        context.RecordBuilder.Add(fieldReader.GetField());

                        fieldReader.SetFieldStart(offset);
                        fieldReader.SetBufferPosition(offset);

                        return true;
                    }
                    else if (cPrev == context.ParserConfiguration.Quote)
                    {
                        // We're out of quotes. Read the reset of
                        // the field like a normal field.
                        return ReadField();
                    }
                }
            }
        }

        protected virtual bool ReadField()
        {
            if (c != context.ParserConfiguration.Delimiter[0] && c != '\r' && c != '\n')
            {
                if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                {
                    // End of file.
                    fieldReader.SetFieldEnd();

                    if (c == ' ' && (context.ParserConfiguration.TrimOptions & TrimOptions.Trim) == TrimOptions.Trim)
                    {
                        fieldReader.SetFieldStart();
                    }

                    context.RecordBuilder.Add(fieldReader.GetField());
                    return true;
                }

                c = fieldReader.GetChar();
            }

            var inSpaces = false;
            while (true)
            {
                if (c == context.ParserConfiguration.Quote && !context.ParserConfiguration.IgnoreQuotes)
                {
                    context.IsFieldBad = true;
                }

                // Trim end outside of quotes.
                if (!inSpaces && c == ' ' && (context.ParserConfiguration.TrimOptions & TrimOptions.Trim) == TrimOptions.Trim)
                {
                    inSpaces = true;
                    fieldReader.SetFieldEnd(-1);
                    fieldReader.AppendField();
                    fieldReader.SetFieldStart(-1);
                    fieldReader.SetRawRecordStart(-1);
                }
                else if (inSpaces && c != ' ')
                {
                    // Hit a non-space char.
                    // Need to determine if it's the end of the field or another char.
                    inSpaces = false;
                    if (c == context.ParserConfiguration.Delimiter[0] || c == '\r' || c == '\n')
                    {
                        fieldReader.SetFieldStart(-1);
                    }
                }

                if (c == context.ParserConfiguration.Delimiter[0])
                {
                    fieldReader.SetFieldEnd(-1);

                    // End of field.
                    if (ReadDelimiter())
                    {
                        // Set the end of the field to the char before the delimiter.
                        context.RecordBuilder.Add(fieldReader.GetField());

                        return false;
                    }
                }
                else if (c == '\r' || c == '\n')
                {
                    // End of line.
                    fieldReader.SetFieldEnd(-1);
                    var offset = ReadLineEnding();
                    fieldReader.SetRawRecordEnd(offset);
                    context.RecordBuilder.Add(fieldReader.GetField());

                    fieldReader.SetFieldStart(offset);
                    fieldReader.SetBufferPosition(offset);

                    return true;
                }

                if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                {
                    // End of file.
                    fieldReader.SetFieldEnd();

                    if (c == ' ' && (context.ParserConfiguration.TrimOptions & TrimOptions.Trim) == TrimOptions.Trim)
                    {
                        fieldReader.SetFieldStart();
                    }

                    context.RecordBuilder.Add(fieldReader.GetField());

                    return true;
                }

                c = fieldReader.GetChar();
            }
        }

        protected virtual bool ReadDelimiter()
        {
            if (c != context.ParserConfiguration.Delimiter[0])
            {
                throw new InvalidOperationException("Tried reading a delimiter when the first delimiter char didn't match the current char.");
            }

            if (context.ParserConfiguration.Delimiter.Length == 1)
            {
                return true;
            }

            for (var i = 1; i < context.ParserConfiguration.Delimiter.Length; i++)
            {
                if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                {
                    // End of file.
                    return false;
                }

                c = fieldReader.GetChar();
                if (c != context.ParserConfiguration.Delimiter[i])
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual bool ReadSpaces()
        {
            while (true)
            {
                if (c != ' ')
                {
                    break;
                }

                if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                {
                    // End of file.
                    return false;
                }

                c = fieldReader.GetChar();
            }

            return true;
        }

        protected virtual void ReadBlankLine()
        {
            if (context.ParserConfiguration.IgnoreBlankLines)
            {
                context.Row++;
            }

            while (true)
            {
                if (c == '\r' || c == '\n')
                {
                    ReadLineEnding();
                    fieldReader.SetFieldStart();
                    return;
                }

                // If the buffer runs, it appends the current data to the field.
                // We don't want to capture any data on a blank line, so we
                // need to set the field start every char.
                fieldReader.SetFieldStart();

                if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                {
                    // End of file.
                    return;
                }

                c = fieldReader.GetChar();
            }
        }

        protected virtual int ReadLineEnding()
        {
            if (c != '\r' && c != '\n')
            {
                throw new InvalidOperationException("Tried reading a line ending when the current char is not a \\r or \\n.");
            }

            var fieldStartOffset = 0;
            if (c == '\r')
            {
                if (fieldReader.IsBufferEmpty && !fieldReader.FillBuffer())
                {
                    // End of file.
                    return fieldStartOffset;
                }

                c = fieldReader.GetChar();
                if (c != '\n' && c != -1)
                {
                    // The start needs to be moved back.
                    fieldStartOffset--;
                }
            }

            return fieldStartOffset;
        }

    }


    public partial class CsvFieldReader : IFieldReader
    {
        private ReadingContext context;
        private bool disposed;

        /// <summary>
        /// Gets the reading context.
        /// </summary>
        public ReadingContext Context {
			get{
				return context;
			}
		}

        /// <summary>
        /// Gets a value indicating if the buffer is empty.
        /// True if the buffer is empty, otherwise false.
        /// </summary>
        public bool IsBufferEmpty
        {
            get { return context.BufferPosition >= context.CharsRead; }
        }

        /// <summary>
        /// Fills the buffer.
        /// </summary>
        /// <returns>True if there is more data left.
        /// False if all the data has been read.</returns>
        public bool FillBuffer()
        {
            try
            {
                if (!IsBufferEmpty)
                {
                    return false;
                }

                if (context.Buffer.Length == 0)
                {
                    context.Buffer = new char[context.ParserConfiguration.BufferSize];
                }
                
                if (context.CharsRead > 0)
                {
                    // Create a new buffer with extra room for what is left from
                    // the old buffer. Copy the remaining contents onto the new buffer.
                    var charactersUsed = Math.Min(context.FieldStartPosition, context.RawRecordStartPosition);
                    var bufferLeft = context.CharsRead - charactersUsed;
                    var bufferUsed = context.CharsRead - bufferLeft;
                    var tempBuffer = new char[bufferLeft + context.ParserConfiguration.BufferSize];
                    Array.Copy(context.Buffer, charactersUsed, tempBuffer, 0, bufferLeft);
                    context.Buffer = tempBuffer;

                    context.BufferPosition = context.BufferPosition - bufferUsed;
                    context.FieldStartPosition = context.FieldStartPosition - bufferUsed;
                    context.FieldEndPosition = Math.Max(context.FieldEndPosition - bufferUsed, 0);
                    context.RawRecordStartPosition = context.RawRecordStartPosition - bufferUsed;
                    context.RawRecordEndPosition = context.RawRecordEndPosition - bufferUsed;
                }

                context.CharsRead = context.Reader.Read(context.Buffer, context.BufferPosition,
                    context.ParserConfiguration.BufferSize);
                if (context.CharsRead == 0)
                {
                    // End of file
                    return false;
                }

                // Add the char count from the previous buffer that was copied onto this one.
                context.CharsRead += context.BufferPosition;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Fills the buffer.
        /// </summary>
        /// <returns>True if there is more data left.
        /// False if all the data has been read.</returns>
        public async Task<bool> FillBufferAsync()
        {
            if (!IsBufferEmpty)
            {
                return false;
            }

            if (context.Buffer.Length == 0)
            {
                context.Buffer = new char[context.ParserConfiguration.BufferSize];
            }

            if (context.CharsRead > 0)
            {
                // Create a new buffer with extra room for what is left from
                // the old buffer. Copy the remaining contents onto the new buffer.
                var charactersUsed = Math.Min(context.FieldStartPosition, context.RawRecordStartPosition);
                var bufferLeft = context.CharsRead - charactersUsed;
                var bufferUsed = context.CharsRead - bufferLeft;
                var tempBuffer = new char[bufferLeft + context.ParserConfiguration.BufferSize];
                Array.Copy(context.Buffer, charactersUsed, tempBuffer, 0, bufferLeft);
                context.Buffer = tempBuffer;

                context.BufferPosition = context.BufferPosition - bufferUsed;
                context.FieldStartPosition = context.FieldStartPosition - bufferUsed;
                context.FieldEndPosition = Math.Max(context.FieldEndPosition - bufferUsed, 0);
                context.RawRecordStartPosition = context.RawRecordStartPosition - bufferUsed;
                context.RawRecordEndPosition = context.RawRecordEndPosition - bufferUsed;
            }

            context.CharsRead = await context.Reader.ReadAsync(context.Buffer, context.BufferPosition, context.ParserConfiguration.BufferSize).ConfigureAwait(false);
            if (context.CharsRead == 0)
            {
                // End of file
                return false;
            }

            // Add the char count from the previous buffer that was copied onto this one.
            context.CharsRead += context.BufferPosition;

            return true;
        }


        public CsvFieldReader(TextReader reader, Configuration configuration) : this(reader, configuration, false) { }


        public CsvFieldReader(TextReader reader, Configuration configuration, bool leaveOpen)
        {
            context = new ReadingContext(reader, configuration, leaveOpen);
        }

        /// <summary>
        /// Gets the next char as an <see cref="int"/>.
        /// </summary>
        public int GetChar()
        {
            var c = context.Buffer[context.BufferPosition];
            context.BufferPosition++;
            context.RawRecordEndPosition = context.BufferPosition;

            context.CharPosition++;

            return c;
        }

        /// <summary>
        /// Gets the field. This will append any reading progress.
        /// </summary>
        /// <returns>The current field.</returns>
        public string GetField()
        {
            AppendField();

            context.IsFieldBad = false;

            var result = context.FieldBuilder.ToString();
            context.FieldBuilder.Clear();

            return result;
        }

        /// <summary>
        /// Appends the current reading progress.
        /// </summary>
        public void AppendField()
        {
            context.RawRecordBuilder.Append(new string(context.Buffer, context.RawRecordStartPosition, context.RawRecordEndPosition - context.RawRecordStartPosition));
            context.RawRecordStartPosition = context.RawRecordEndPosition;

            var length = context.FieldEndPosition - context.FieldStartPosition;
            context.FieldBuilder.Append(new string(context.Buffer, context.FieldStartPosition, length));
            context.FieldStartPosition = context.BufferPosition;
            context.FieldEndPosition = 0;
        }

        /// <summary>
        /// Move's the buffer position according to the given offset.
        /// </summary>
        /// <param name="offset">The offset to move the buffer.</param>
        public void SetBufferPosition(int offset = 0)
        {
            var position = context.BufferPosition + offset;
            if (position >= 0)
            {
                context.BufferPosition = position;
            }
        }

        /// <summary>
        /// Sets the start of the field to the current buffer position.
        /// </summary>
        /// <param name="offset">An offset for the field start.
        /// The offset should be less than 1.</param>
        public void SetFieldStart(int offset = 0)
        {
            var position = context.BufferPosition + offset;
            if (position >= 0)
            {
                context.FieldStartPosition = position;
            }
        }

        /// <summary>
        /// Sets the end of the field to the current buffer position.
        /// </summary>
        /// <param name="offset">An offset for the field start.
        /// The offset should be less than 1.</param>
        public void SetFieldEnd(int offset = 0)
        {
            var position = context.BufferPosition + offset;
            if (position >= 0)
            {
                context.FieldEndPosition = position;
            }
        }

        /// <summary>
        /// Sets the raw recodr start to the current buffer position;
        /// </summary>
        /// <param name="offset">An offset for the raw record start.
        /// The offset should be less than 1.</param>
        public void SetRawRecordStart(int offset)
        {
            var position = context.BufferPosition + offset;
            if (position >= 0)
            {
                context.RawRecordStartPosition = position;
            }
        }

        /// <summary>
        /// Sets the raw record end to the current buffer position.
        /// </summary>
        /// <param name="offset">An offset for the raw record end.
        /// The offset should be less than 1.</param>
        public void SetRawRecordEnd(int offset)
        {
            var position = context.BufferPosition + offset;
            if (position >= 0)
            {
                context.RawRecordEndPosition = position;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if the instance needs to be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                context.Dispose();
            }

            context = null;
            disposed = true;
        }
    }

    public class Configuration : IReaderConfiguration
    {
        private string delimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
        private char escape = '"';
        private char quote = '"';
        private string quoteString = "\"";
        private string doubleQuoteString = "\"\"";
        private char[] quoteRequiredChars;
        private CultureInfo cultureInfo = CultureInfo.CurrentCulture;
        private bool quoteAllFields;
        private bool quoteNoFields;

        /// <summary>
        /// Gets or sets a value indicating if the
        /// CSV file has a header record.
        /// Default is true.
        /// </summary>
        public bool HasHeaderRecord
        {
            get { return hasHeaderRecord; }
            set { hasHeaderRecord = value; }
        }

        private bool hasHeaderRecord = true;

        public Action<ReadingContext> BadDataFound { get; set; }

        /// <summary>
		/// Builds the values for the RequiredQuoteChars property.
		/// </summary>
		public Func<char[]> BuildRequiredQuoteChars { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if a line break found in a quote field should
        /// be considered bad data. True to consider a line break bad data, otherwise false.
        /// Defaults to false.
        /// </summary>
        public bool LineBreakInQuotedFieldIsBadData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if fields should be sanitized
        /// to prevent malicious injection. This covers MS Excel, 
        /// Google Sheets and Open Office Calc.
        /// </summary>
        public bool SanitizeForInjection { get; set; }

        /// <summary>
        /// Gets or sets the characters that are used for injection attacks.
        /// </summary>
        public char[] InjectionCharacters { 
			get{
				return new[] { '=', '@', '+', '-' };
			}
		}

        /// <summary>
        /// Gets or sets the character used to escape a detected injection.
        /// </summary>
        public char InjectionEscapeCharacter { 
			get{
				return '\t';
			}
		}

        public Func<Type, string, string> ReferenceHeaderPrefix { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether changes in the column
        /// count should be detected. If true, a <see cref="BadDataException"/>
        /// will be thrown if a different column count is detected.
        /// </summary>
        /// <value>
        /// <c>true</c> if [detect column count changes]; otherwise, <c>false</c>.
        /// </value>
        public bool DetectColumnCountChanges { get; set; }

        public void UnregisterClassMap(Type classMapType)
        {
            throw new NotImplementedException();
        }

        public void UnregisterClassMap()
        {
            throw new NotImplementedException();
        }

        public Func<Type, bool> ShouldUseConstructorParameters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether references
        /// should be ignored when auto mapping. True to ignore
        /// references, otherwise false. Default is false.
        /// </summary>
        public bool IgnoreReferences { get; set; }

        public Func<string[], bool> ShouldSkipRecord { get; set; }

        /// <summary>
        /// Gets or sets the field trimming options.
        /// </summary>
        public TrimOptions TrimOptions { get; set; }

        /// <summary>
        /// Gets or sets the delimiter used to separate fields.
        /// Default is CultureInfo.CurrentCulture.TextInfo.ListSeparator.
        /// </summary>
        public string Delimiter
        {
            get { return delimiter; }
            set
            {
                if (value == "\n")
                {
                    throw new ConfigurationException("Newline is not a valid delimiter.");
                }

                if (value == "\r")
                {
                    throw new ConfigurationException("Carriage return is not a valid delimiter.");
                }

                if (value == System.Convert.ToString(quote))
                {
                    throw new ConfigurationException("You can not use the quote as a delimiter.");
                }

                delimiter = value;

                quoteRequiredChars = BuildRequiredQuoteChars();
            }
        }

        /// <summary>
        /// Gets or sets the escape character used to escape a quote inside a field.
        /// Default is '"'.
        /// </summary>
        public char Escape
        {
            get { return escape; }
            set
            {
                if (value == '\n')
                {
                    throw new ConfigurationException("Newline is not a valid escape.");
                }

                if (value == '\r')
                {
                    throw new ConfigurationException("Carriage return is not a valid escape.");
                }

                if (value.ToString() == delimiter)
                {
                    throw new ConfigurationException("You can not use the delimiter as an escape.");
                }

                escape = value;

                doubleQuoteString = escape + quoteString;
            }
        }

        /// <summary>
        /// Gets or sets the character used to quote fields.
        /// Default is '"'.
        /// </summary>
        public char Quote
        {
            get { return quote; }
            set
            {
                if (value == '\n')
                {
                    throw new ConfigurationException("Newline is not a valid quote.");
                }

                if (value == '\r')
                {
                    throw new ConfigurationException("Carriage return is not a valid quote.");
                }

                if (value == '\0')
                {
                    throw new ConfigurationException("Null is not a valid quote.");
                }

                if (System.Convert.ToString(value) == delimiter)
                {
                    throw new ConfigurationException("You can not use the delimiter as a quote.");
                }

                quote = value;

                quoteString = System.Convert.ToString(value, cultureInfo);
                doubleQuoteString = escape + quoteString;
            }
        }

        /// <summary>
        /// Gets a string representation of the currently configured Quote character.
        /// </summary>
        /// <value>
        /// The new quote string.
        /// </value>
        public string QuoteString {
			get {
				return quoteString;
			}
		}

        /// <summary>
        /// Gets a string representation of two of the currently configured Quote characters.
        /// </summary>
        /// <value>
        /// The new double quote string.
        /// </value>
        public string DoubleQuoteString {
			get {
				return doubleQuoteString;
			}
		} 

        /// <summary>
        /// Gets an array characters that require
        /// the field to be quoted.
        /// </summary>
        public char[] QuoteRequiredChars {
			get {
				return quoteRequiredChars;
			}
		}

        /// <summary>
        /// Gets or sets the character used to denote
        /// a line that is commented out. Default is '#'.
        /// </summary>
        public char Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        private char comment = '#';

        /// <summary>
        /// Gets or sets a value indicating if comments are allowed.
        /// True to allow commented out lines, otherwise false.
        /// </summary>
        public bool AllowComments { get; set; }

        /// <summary>
        /// Gets or sets the size of the buffer
        /// used for reading CSV files.
        /// Default is 2048.
        /// </summary>
        public int BufferSize
        {
            get { return bufferSize; }
            set { bufferSize = value; }
        }
        private int bufferSize = 2048;

        /// <summary>
        /// Gets or sets a value indicating whether all fields are quoted when writing,
        /// or just ones that have to be. <see cref="QuoteAllFields"/> and
        /// <see cref="QuoteNoFields"/> cannot be true at the same time. Turning one
        /// on will turn the other off.
        /// </summary>
        /// <value>
        ///   <c>true</c> if all fields should be quoted; otherwise, <c>false</c>.
        /// </value>
        public bool QuoteAllFields
        {
            get { return quoteAllFields; }
            set
            {
                quoteAllFields = value;
                if (quoteAllFields && quoteNoFields)
                {
                    // Both can't be true at the same time.
                    quoteNoFields = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether no fields are quoted when writing.
        /// <see cref="QuoteAllFields"/> and <see cref="QuoteNoFields"/> cannot be true 
        /// at the same time. Turning one on will turn the other off.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [quote no fields]; otherwise, <c>false</c>.
        /// </value>
        public bool QuoteNoFields
        {
            get { return quoteNoFields; }
            set
            {
                quoteNoFields = value;
                if (quoteNoFields && quoteAllFields)
                {
                    // Both can't be true at the same time.
                    quoteAllFields = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the number of bytes should
        /// be counted while parsing. Default is false. This will slow down parsing
        /// because it needs to get the byte count of every char for the given encoding.
        /// The <see cref="Encoding"/> needs to be set correctly for this to be accurate.
        /// </summary>
        public bool CountBytes { get; set; }

        /// <summary>
        /// Gets or sets the encoding used when counting bytes.
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }set { encoding = value; }
        }

        private Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// Gets or sets the culture info used to read an write CSV files.
        /// </summary>
        public CultureInfo CultureInfo
        {
            get { return cultureInfo; }
            set { cultureInfo = value; }
        }

        public Func<string, int, string> PrepareHeaderForMatch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if quotes should be
        /// ignored when parsing and treated like any other character.
        /// </summary>
        public bool IgnoreQuotes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if private
        /// member should be read from and written to.
        /// True to include private member, otherwise false. Default is false.
        /// </summary>
        public bool IncludePrivateMembers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if blank lines
        /// should be ignored when reading.
        /// True to ignore, otherwise false. Default is true.
        /// </summary>
        public bool IgnoreBlankLines
        {
            get { return ignoreBlankLines; }
            set { ignoreBlankLines = value; }
        }

        private bool ignoreBlankLines = true;
    }

    public interface IFieldReader : IDisposable
    {
        /// <summary>
        /// Gets the reading context.
        /// </summary>
        ReadingContext Context { get; }

        /// <summary>
        /// Gets a value indicating if the buffer is empty.
        /// True if the buffer is empty, otherwise false.
        /// </summary>
        bool IsBufferEmpty { get; }

        /// <summary>
        /// Fills the buffer.
        /// </summary>
        /// <returns>True if there is more data left.
        /// False if all the data has been read.</returns>
        bool FillBuffer();

        /// <summary>
        /// Fills the buffer asynchronously.
        /// </summary>
        /// <returns>True if there is more data left.
        /// False if all the data has been read.</returns>
        Task<bool> FillBufferAsync();

        /// <summary>
        /// Gets the next char as an <see cref="int"/>.
        /// </summary>
        int GetChar();

        /// <summary>
        /// Gets the field. This will append any reading progress.
        /// </summary>
        /// <returns>The current field.</returns>
        string GetField();

        /// <summary>
        /// Appends the current reading progress.
        /// </summary>
        void AppendField();

        /// <summary>
        /// Move's the buffer position according to the given offset.
        /// </summary>
        /// <param name="offset">The offset to move the buffer.</param>
        void SetBufferPosition(int offset = 0);

        /// <summary>
        /// Sets the start of the field to the current buffer position.
        /// </summary>
        /// <param name="offset">An offset for the field start.
        /// The offset should be less than 1.</param>
        void SetFieldStart(int offset = 0);

        /// <summary>
        /// Sets the end of the field to the current buffer position.
        /// </summary>
        /// <param name="offset">An offset for the field start.
        /// The offset should be less than 1.</param>
        void SetFieldEnd(int offset = 0);

        /// <summary>
        /// Sets the raw recodr start to the current buffer position;
        /// </summary>
        /// <param name="offset">An offset for the raw record start.
        /// The offset should be less than 1.</param>
        void SetRawRecordStart(int offset);

        /// <summary>
        /// Sets the raw record end to the current buffer position.
        /// </summary>
        /// <param name="offset">An offset for the raw record end.
        /// The offset should be less than 1.</param>
        void SetRawRecordEnd(int offset);
    }

    public class RecordBuilder
    {
        private const int DEFAULT_CAPACITY = 16;
        private string[] record;
        private int position;
        private int capacity;

        /// <summary>
        /// The number of records.
        /// </summary>
        public int Length{
			get{
				return position;
			}
		}

        /// <summary>
        /// The total record capacity.
        /// </summary>
        public int Capacity{
			get{
				return capacity;
			}
		}

        /// <summary>
        /// Creates a new <see cref="RecordBuilder"/> using defaults.
        /// </summary>
        public RecordBuilder() : this(DEFAULT_CAPACITY) { }

        /// <summary>
        /// Creatse a new <see cref="RecordBuilder"/> using the given capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity.</param>
        public RecordBuilder(int capacity)
        {
            this.capacity = capacity > 0 ? capacity : DEFAULT_CAPACITY;

            record = new string[capacity];
        }

        /// <summary>
        /// Adds a new field to the <see cref="RecordBuilder"/>.
        /// </summary>
        /// <param name="field">The field to add.</param>
        /// <returns>The current instance of the <see cref="RecordBuilder"/>.</returns>
        public RecordBuilder Add(string field)
        {
            if (position == record.Length)
            {
                capacity = capacity * 2;
                Array.Resize(ref record, capacity);
            }

            record[position] = field;
            position++;

            return this;
        }

        /// <summary>
        /// Clears the records.
        /// </summary>
        /// <returns>The current instance of the <see cref="RecordBuilder"/>.</returns>
        public RecordBuilder Clear()
        {
            position = 0;

            return this;
        }

        /// <summary>
        /// Returns the record as an <see cref="T:string[]"/>.
        /// </summary>
        /// <returns>The record as an <see cref="T:string[]"/>.</returns>
        public string[] ToArray()
        {
            var array = new string[position];
            Array.Copy(record, array, position);

            return array;
        }
    }

    public class ReadingContext : IDisposable
    {
        private bool disposed;
        private readonly Configuration configuration;

        /// <summary>
        /// Gets the raw record builder.
        /// </summary>
        public StringBuilder RawRecordBuilder = new StringBuilder();

        /// <summary>
        /// Gets the field builder.
        /// </summary>
        public StringBuilder FieldBuilder = new StringBuilder();

        public IParserConfiguration ParserConfiguration{
			get{
				return configuration;
			}
		}


        /// <summary>
        /// Gets the named indexes.
        /// </summary>
        public Dictionary<string, List<int>> NamedIndexes = new Dictionary<string, List<int>>();


        /// <summary>
        /// Gets the create record functions.
        /// </summary>
        public Dictionary<Type, Delegate> CreateRecordFuncs = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Gets the hydrate record actions.
        /// </summary>
        public Dictionary<Type, Delegate> HydrateRecordActions = new Dictionary<Type, Delegate>();


        /// <summary>
        /// Gets the <see cref="TextReader"/> that is read from.
        /// </summary>
        public TextReader Reader;

        /// <summary>
        /// Gets a value indicating if the <see cref="Reader"/>
        /// should be left open when disposing.
        /// </summary>
        public bool LeaveOpen;

        /// <summary>
        /// Gets the buffer used to store data from the <see cref="Reader"/>.
        /// </summary>
        public char[] Buffer;

        /// <summary>
        /// Gets the buffer position.
        /// </summary>
        public int BufferPosition;

        /// <summary>
        /// Gets the field start position.
        /// </summary>
        public int FieldStartPosition;

        /// <summary>
        /// Gets the field end position.
        /// </summary>
        public int FieldEndPosition;

        /// <summary>
        /// Gets the raw record start position.
        /// </summary>
        public int RawRecordStartPosition;

        /// <summary>
        /// Gets the raw record end position.
        /// </summary>
        public int RawRecordEndPosition;

        /// <summary>
        /// Gets the number of characters read from the <see cref="Reader"/>.
        /// </summary>
        public int CharsRead;

        /// <summary>
        /// Gets the character position.
        /// </summary>
        public long CharPosition;

        /// <summary>
        /// Gets the byte position.
        /// </summary>
        public long BytePosition;

        /// <summary>
        /// Gets a value indicating if the field is bad.
        /// True if the field is bad, otherwise false.
        /// A field is bad if a quote is found in a field
        /// that isn't escaped.
        /// </summary>
        public bool IsFieldBad;

        /// <summary>
        /// Gets the record.
        /// </summary>
        public string[] Record;

        /// <summary>
        /// Gets the row of the CSV file that the parser is currently on.
        /// </summary>
        public int Row;

        /// <summary>
        /// Gets the row of the CSV file that the parser is currently on.
        /// This is the actual file row.
        /// </summary>
        public int RawRow;

        /// <summary>
        /// Gets a value indicating if reading has begun.
        /// </summary>
        public bool HasBeenRead;

        /// <summary>
        /// Gets the header record.
        /// </summary>
        public string[] HeaderRecord;

        /// <summary>
        /// Gets the current index.
        /// </summary>
        public int CurrentIndex = -1;

        /// <summary>
        /// Gets the column count.
        /// </summary>
        public int ColumnCount;

        /// <summary>
        /// Gets all the characters of the record including
        /// quotes, delimeters, and line endings.
        /// </summary>
        public string RawRecord {
			get{
				return RawRecordBuilder.ToString();
			}
		}

        /// <summary>
        /// Gets the field.
        /// </summary>
        public string Field{
			get{
				return FieldBuilder.ToString();
			}
		}

        public RecordBuilder RecordBuilder = new RecordBuilder();

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="leaveOpen">A value indicating if the TextReader should be left open when disposing.</param>
        public ReadingContext(TextReader reader, Configuration configuration, bool leaveOpen)
        {
            Reader = reader;
            this.configuration = configuration;
            LeaveOpen = leaveOpen;
            Buffer = new char[0];
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if the instance needs to be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                Reader.Dispose();
            }

            Reader = null;
            disposed = true;
        }
    }

    public interface IParserConfiguration
    {
        /// <summary>
        /// Gets or sets the size of the buffer
        /// used for reading CSV files.
        /// Default is 2048.
        /// </summary>
        int BufferSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the number of bytes should
        /// be counted while parsing. Default is false. This will slow down parsing
        /// because it needs to get the byte count of every char for the given encoding.
        /// The <see cref="Encoding"/> needs to be set correctly for this to be accurate.
        /// </summary>
        bool CountBytes { get; set; }

        /// <summary>
        /// Gets or sets the encoding used when counting bytes.
        /// </summary>
        Encoding Encoding { get; set; }

        /// <summary>
        /// Gets or sets the function that is called when bad field data is found. A field
        /// has bad data if it contains a quote and the field is not quoted (escaped).
        /// You can supply your own function to do other things like logging the issue
        /// instead of throwing an exception.
        /// Arguments: context
        /// </summary>
        Action<ReadingContext> BadDataFound { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if a line break found in a quote field should
        /// be considered bad data. True to consider a line break bad data, otherwise false.
        /// Defaults to false.
        /// </summary>
        bool LineBreakInQuotedFieldIsBadData { get; set; }

        /// <summary>
        /// Gets or sets the character used to denote
        /// a line that is commented out. Default is '#'.
        /// </summary>
        char Comment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if comments are allowed.
        /// True to allow commented out lines, otherwise false.
        /// </summary>
        bool AllowComments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if blank lines
        /// should be ignored when reading.
        /// True to ignore, otherwise false. Default is true.
        /// </summary>
        bool IgnoreBlankLines { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if quotes should be
        /// ingored when parsing and treated like any other character.
        /// </summary>
        bool IgnoreQuotes { get; set; }

        /// <summary>
        /// Gets or sets the character used to quote fields.
        /// Default is '"'.
        /// </summary>
        char Quote { get; set; }

        /// <summary>
        /// Gets or sets the delimiter used to separate fields.
        /// Default is CultureInfo.CurrentCulture.TextInfo.ListSeparator.
        /// </summary>
        string Delimiter { get; set; }

        /// <summary>
        /// Gets or sets the escape character used to escape a quote inside a field.
        /// Default is '"'.
        /// </summary>
        char Escape { get; set; }

        /// <summary>
        /// Gets or sets the field trimming options.
        /// </summary>
        TrimOptions TrimOptions { get; set; }
    }

    [Flags]
    public enum TrimOptions
    {
        /// <summary>
        /// No trimming.
        /// </summary>
        None = 0,

        /// <summary>
        /// Trims the whitespace around a field.
        /// </summary>
        Trim = 1,

        /// <summary>
        /// Trims the whitespace inside of quotes around a field.
        /// </summary>
        InsideQuotes = 2
    }

    public interface IReaderConfiguration : IParserConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating if the
        /// CSV file has a header record.
        /// Default is true.
        /// </summary>
        bool HasHeaderRecord { get; set; }


        /// <summary>
        /// Gets or sets the culture info used to read an write CSV files.
        /// </summary>
        CultureInfo CultureInfo { get; set; }

        /// <summary>
        /// Prepares the header field for matching against a member name.
        /// The header field and the member name are both ran through this function.
        /// You should do things like trimming, removing whitespace, removing underscores,
        /// and making casing changes to ignore case.
        /// </summary>
        Func<string, int, string> PrepareHeaderForMatch { get; set; }

        /// <summary>
        /// Determines if constructor parameters should be used to create
        /// the class instead of the default constructor and members.
        /// </summary>
        Func<Type, bool> ShouldUseConstructorParameters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether references
        /// should be ignored when auto mapping. True to ignore
        /// references, otherwise false. Default is false.
        /// </summary>
        bool IgnoreReferences { get; set; }

        /// <summary>
        /// Gets or sets the callback that will be called to
        /// determine whether to skip the given record or not.
        /// </summary>
        Func<string[], bool> ShouldSkipRecord { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if private
        /// member should be read from and written to.
        /// True to include private member, otherwise false. Default is false.
        /// </summary>
        bool IncludePrivateMembers { get; set; }

        /// <summary>
        /// Gets or sets a callback that will return the prefix for a reference header.
        /// Arguments: memberType, memberName
        /// </summary>
        Func<Type, string, string> ReferenceHeaderPrefix { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether changes in the column
        /// count should be detected. If true, a <see cref="BadDataException"/>
        /// will be thrown if a different column count is detected.
        /// </summary>
        /// <value>
        /// <c>true</c> if [detect column count changes]; otherwise, <c>false</c>.
        /// </value>
        bool DetectColumnCountChanges { get; set; }

        /// <summary>
        /// Unregisters the class map.
        /// </summary>
        /// <param name="classMapType">The map type to unregister.</param>
        void UnregisterClassMap(Type classMapType);

        /// <summary>
        /// Unregisters all class maps.
        /// </summary>
        void UnregisterClassMap();
    }

    [Serializable]
    public class ConfigurationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
        /// </summary>
        public ConfigurationException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConfigurationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationException"/> class
        /// with a specified error message and a reference to the inner exception that 
        /// is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}
