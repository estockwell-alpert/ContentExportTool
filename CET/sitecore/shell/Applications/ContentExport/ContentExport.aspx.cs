using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace ContentExportTool
{
    public partial class ContentExport : Page
    {
        private string ItemNotFoundText = "[Item not found]";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {

            }
        }

        private string exportOutput;

        protected void btnRunExport_OnClick(object sender, EventArgs e)
        {
            exportOutput = "";

            GetItems();

        }

        public void GetItems()
        {

            _fieldsList = new List<FieldData>();

            var fields = inputFields.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var templates = inputTemplates.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            var startPath = inputStartitem.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(); ;

            if (chkAllFields.Checked)
            {
                var fieldtemplates = txtFieldTemplates.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

                foreach (var templateName in fieldtemplates)
                {
                    try
                    {
                        var fieldsForTemplate = GetAllFields(templateName);
                        fields.AddRange(fieldsForTemplate);
                    }
                    catch (Exception ex)
                    {
                        // log error
                    }
                }
            }

            // duplicate fields will cause errors
            fields = fields.Distinct().ToList();

            var templatesFragment = "";
            foreach (var template in templates)
            {
                var templateFragment = @"{
                          name: ""_templates""
                           value: """ + template + @"""
                           operator: CONTAINS
                        }";

                templatesFragment += templateFragment;
            }

            var pathsFragment = "";
            foreach (var path in startPath)
            {
                var pathFragment = @"{
                   name: ""_path""
                   value: """ + path + @"""
                   operator: CONTAINS
                 }";

                pathsFragment += pathFragment;
            }

            var fieldsFragment = "";
            foreach (var field in fields)
            {
                var fieldFragment = $"{field}: field(name: \"{field}\") {{ value }}";

                fieldsFragment += fieldFragment;
            }


            var query = @"query {
              pageOne: search(
                 where: {
                   AND: [
                        {
                            OR: [
                                " + templatesFragment + @"
                            ]
                        }
                        {
                            OR:[
                                " + pathsFragment + @"
                            ]
                        }
                      ]
                     }
                   first: 1000
                   ) {
                    total
                    pageInfo {
                        endCursor
                        hasNext
                     }
                    results {
                        name
                        id
                       url {
                            path
                       }
                       " + fieldsFragment + @"
                    }
                }
             }";

            var result = GetGQLData(query);

            var data = DeserializeResponse(result, fields);

            Sitecore.Diagnostics.Log.Info(result, this);
        }

        private string GetGQLData(string query)
        {
            try
            {
                var gqlEndpoint = "https://edge.sitecorecloud.io/api/graphql/v1";
                var apiKey = "[your api key]";

                var request = (HttpWebRequest)WebRequest.Create(gqlEndpoint);
                request.Method = "POST";
                request.ContentType = "application/graphql";
                request.ContentLength = query.Length;
                request.Headers["sc_apikey"] = apiKey;

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(query);
                }

                var httpResponse = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private List<string> GetAllFields(string templateName)
        {
            var query = @"query {
                __type(name:""" + templateName + @""") {
                    fields {
                        name
                        description
                    }  
                }
            }";

            var json = GetGQLData(query);
            var response = JsonConvert.DeserializeObject<GraphQLFieldsResponse>(json);

            var fieldNames = response.data.__type.fields.Select(x => x.name).ToList();
            return fieldNames;
        }

        private List<ResultData> DeserializeResponse(string data, List<string> fields)
        {
            List<ResultData> results = new List<ResultData>();

            var responseObj = JsonConvert.DeserializeObject<GraphQlResponse>(data);

            foreach (var entry in responseObj.data.pageOne.results)
            {
                var result = new ResultData
                {
                    Name = entry["name"],
                    Id = entry["id"],
                    Url = entry["url"]["path"],
                    Fields = new Dictionary<string, string>()
                };

                foreach (var field in fields)
                {
                    var fieldObj = entry[field];

                    if (fieldObj == null)
                    {
                        result.Fields[field] = "n/a";
                    }
                    else
                    {
                        var fieldValue = fieldObj["value"];
                        result.Fields[field] = fieldValue;
                    }
                }

                results.Add(result);
            }

            return results;
        }
    }

    public class GraphQLFieldsResponse
    {
        public GraphQlSchemaData data;
    }

    public class GraphQlSchemaData
    {
        public GraphQLType __type;
    }

    public class GraphQLType
    {
        public List<GraphQLField> fields;
    }

    public class GraphQLField
    {
        public string name;
        public string description;
    }

    public class GraphQlResponse
    {
        public GQLData data { get; set; }
    }

    public class GQLData
    {
        public GQLPage pageOne { get; set; }
    }

    public class GQLPage
    {
        public int total { get; set; }
        public dynamic[] results { get; set; }
    }

    public class FieldData
    {
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

    public class ResultData
    {
        public string Name;
        public string Url;
        public string Id;
        public Dictionary<string, string> Fields;
    }
}