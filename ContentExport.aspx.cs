using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Web;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using System.Net.Http;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;

namespace ContentExportTool
{
	public partial class ContentExport : System.Web.UI.Page
	{
		// TODO: 
		// 1. export results to CSV ✔
		// 2. Build Field selector using javascript and schema query
		// 3. Import section

		private List<FieldData> _fieldsList;
		private string ItemNotFoundText = "[Item not found]";

		protected void Page_Load(object sender, EventArgs e)
		{
			litFeedback.Text = "";
			PhBrowseFields.Visible = false;
			PhBrowseModal.Visible = false;

			if (!IsPostBack)
			{

			}
		}

		private string exportOutput;

		protected void btnRunExport_OnClick(object sender, EventArgs e)
		{
			try
			{
				exportOutput = "";

				GetItems();
			}
			catch (Exception ex)
			{
				litFeedback.Text = ex.Message;
			}
		}

		public void GetItems()
		{

			_fieldsList = new List<FieldData>();

			var fields = inputFields.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
			var templates = inputTemplates.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
			var startPath = inputStartitem.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(); ;

			if (chkAllFields.Checked)
			{
				fields = GetAvailableFields(txtFieldTemplates.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList());
			}
			// duplicate fields will cause errors
			fields = fields.Distinct().ToList();

			var query = GetQuery(fields, templates, startPath);

			var result = GetGQLData(query);

			var data = DeserializeResponse(result, fields);

			CreateCsv(data, fields);
		}

		#region Generate CSV file
		private void CreateCsv(List<ResultData> data, List<string> fields)
		{
			StartResponse("ContentExport");

			using (StringWriter sw = new StringWriter())
			{
				var headingString = "Item Path,"
									+ "Name,"
									+ "ID,";

				foreach (var field in fields)
				{
					headingString += $"{field},";
				}
				sw.WriteLine(headingString);

				foreach (var item in data)
				{
					Guid id = Guid.Empty;
					Guid.TryParse(item.Id, out id);
					var itemLine = $"{item.Url},{item.Name},{id},";
					foreach (var field in fields)
					{
						var value = item.Fields[field];
						itemLine += $"{value},";
					}
					sw.WriteLine(itemLine);
				}

				SetCookieAndResponse(sw.ToString());
			}
		}
		#endregion

		#region GQL calls
		public string GetToken()
		{
			var token = string.Empty;

			using (var httpClient = new HttpClient())
			{
				httpClient.BaseAddress = new Uri(txtIdentityServerUrl.Value);

				var passwordRequest = new PasswordTokenRequest()
				{
					Address = "/connect/token",
					ClientId = txtClientId.Value, //"contentexporttool",
					ClientSecret = txtClientSecret.Value, //CXYSCLCzN0ilhep5sthA1tvL6rZJnxcakSEhLTlMb3lHTpqSCEJlmeKlTG9eKBFcuGt2uQO0rqVuv0Msch7qfoeR3XmTU9Dm4pqe
					GrantType = IdentityModel.OidcConstants.GrantTypes.Password,
					Scope = "openid sitecore.profile sitecore.profile.api",
					UserName = $"sitecore\\{txtUsername.Value}",
					Password = txtPassword.Value
				};

				var tokenResult = httpClient.RequestPasswordTokenAsync(passwordRequest).Result;
				token = tokenResult?.AccessToken ?? tokenResult.HttpStatusCode.ToString();
				return token;
			}
		}

		public string GetQuery(List<string> fields, List<string> templates, List<string> startPath)
		{
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
				// remove basic fields that we already include in query
				if (field == "id" || field == "name" || field == "url")
					continue;

				var fieldFragment =
					field + @": field(name: """ + field + @""") {
                        value                
                    }
                    ";

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

			return query;
		}

		public string ExecuteQueryTest(string query)
		{
			var token = GetToken();

			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

				var requestPayload = new
				{
					query = query
				};

				var jsonPayload = JObject.FromObject(requestPayload).ToString();
				var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
				var response = client.PostAsync(txtGqlEndpoint.Value, content).Result;
				var responseContent = response.Content.ReadAsStringAsync().Result;

				if (!response.IsSuccessStatusCode)
				{
					throw new Exception($"Error: {response.StatusCode}\n{responseContent}");
				}
				return responseContent;
			}
		}

		private string GetGQLData(string query, string authorizationToken = "")
		{
			try
			{
				var gqlEndpoint = txtGqlEndpoint.Value; //"https://edge.sitecorecloud.io/api/graphql/v1";
				var apiKey = txtSCApiKey.Value; //"YkR0MnJMbXpTR3U5WGRXdUppRGx0cGx0VG9xMEJyVzgwVEhGNTBVK3dtTT18Y25oLTU1ODU1NTNm";

				var request = (HttpWebRequest)WebRequest.Create(gqlEndpoint);
				request.Method = "POST";
				request.ContentType = "application/graphql";
				request.ContentLength = query.Length;

				if (String.IsNullOrEmpty(authorizationToken))
				{
					request.Headers["sc_apikey"] = apiKey;
				}else
				{
					request.Headers["Authorization"] = $"Bearer {authorizationToken}";
				}

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
		#endregion

		#region Import
		protected void btnBeginImport_OnClick(object sender, EventArgs e)
		{
			ProcessImport();
		}

		protected void ProcessImport()
		{
			litFeedback.Text = "";
			var createItems = radImport.Checked;
			var updateItems = radUpdate.Checked;
			var publishOnly = radPublish.Checked;
			var deleteItems = radDelete.Checked;
			PhBrowseModal.Visible = false;
			PhBrowseFields.Visible = false;

			try
			{
				var output = "";
				var file = btnFileUpload.PostedFile;
				if (file == null || String.IsNullOrEmpty(file.FileName))
				{
					litUploadResponse.Text = "You must select a file first<br/>";
					return;
				}

				string extension = System.IO.Path.GetExtension(file.FileName);
				if (extension.ToLower() != ".csv")
				{
					litUploadResponse.Text = "Upload file must be in CSV format<br/>";
					return;
				}

				var authToken = GetToken();
				if (String.IsNullOrEmpty(authToken))
				{
					litUploadResponse.Text = "Failed Authentication. Make sure the auth token fields are filled.";
					return;
				}

				var fieldsMap = new List<String>();
				var itemPathIndex = 0;
				var itemNameIndex = 0;
				var itemTemplateIndex = 0;
				var placeholderIndex = 0;
				var renderingIndex = 0;
				var itemsImported = 0;
				var languageIndex = 0;

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
							languageIndex = fieldsMap.FindIndex(x => x.ToLower() == "language");
						}
						else
						{
							var path = cells[itemPathIndex];
							Guid guid;
							if (!Guid.TryParse(path, out guid) && !path.ToLower().StartsWith("/sitecore/"))
							{
								path = "/sitecore/content" + (path.StartsWith("/") ? "" : "/") + path;
							}

							if (createItems)
							{
								var createQuery = GetCreateQuery(cells, itemPathIndex, itemNameIndex, itemTemplateIndex, languageIndex, fieldsMap);

								var response = GetGQLData(createQuery, authToken);

								litFeedback.Text += response + "<br/>";
							}
							else if (updateItems)
							{
								var mutationQuery = GetUpdateQuery(cells, itemPathIndex, itemNameIndex, itemTemplateIndex, languageIndex, fieldsMap);

								// TO DO: Handle Errors - if no version, create a version and then re-run query
								//var response = GetGQLData(mutationQuery, authToken);
								var response = ExecuteQueryTest(mutationQuery);

								litFeedback.Text += response + "<br/>";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				litFeedback.Text = ex.Message;
			}
		}

		private string GetMutationFieldFragment(string[] cells, int itemPathIndex, int itemNameIndex, int itemTemplateIndex, int langIndex, List<string> fieldsMap)
		{
			var fieldsFragments = "";
			for (var i = 0; i < cells.Length; i++)
			{
				if (i == itemPathIndex || i == itemNameIndex || i == itemTemplateIndex || i == langIndex || i > fieldsMap.Count - 1) continue;

				var fieldName = fieldsMap[i];
				var value = cells[i];

				var fieldsFragment = @"
					{ name: """ + fieldName + @""", value: """ + value + @""" }";

				fieldsFragments += fieldsFragment;
			}
			return fieldsFragments;
		}

		private string GetCreateQuery(string[] cells, int itemPathIndex, int itemNameIndex, int itemTemplateIndex, int langIndex, List<string> fieldsMap)
		{
			var fieldsFragments = GetMutationFieldFragment(cells, itemPathIndex, itemNameIndex, itemTemplateIndex, langIndex, fieldsMap);

			var languageFragment = langIndex > -1 && !String.IsNullOrEmpty(cells[langIndex]) ? @"language: """ + cells[langIndex] + @"""" : "";

			var query = @"mutation {
							  createItem(
								input: {
								  name: """ + cells[itemNameIndex] + @"""
								  templateId: """ + cells[itemTemplateIndex] + @"""
								  parent: """ + cells[itemPathIndex] + @"""
								  " + languageFragment + @"
								  fields: [
									" + fieldsFragments + @"
								  ]
								}
							  ) {
								item {
								  itemId
								  name
								  path
								}
							  }
							}";

			return query;
		}

		private string GetUpdateQuery(string[] cells, int itemPathIndex, int itemNameIndex, int itemTemplateIndex, int langIndex, List<string> fieldsMap)
		{
			var fieldsFragments = GetMutationFieldFragment(cells, itemPathIndex, itemNameIndex, itemTemplateIndex, langIndex, fieldsMap);

			var languageFragment = langIndex > -1 && !String.IsNullOrEmpty(cells[langIndex]) ? @"language: """ + cells[langIndex] + @"""" : "";

			var query = @"mutation UpdateItem {
							updateItem(
								input: {
									path: """ + cells[itemPathIndex] + @"""
									" + languageFragment + @"
									fields: [
										" + fieldsFragments + @"
									]
								  }
								)
								{
									item {
										name
									}
								  }
								}";

			return query;
		}
		#endregion

		#region Serialization and Deserialization
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
		#endregion

		#region Response
		protected void StartResponse(string fileName)
		{
			Response.Clear();
			Response.Buffer = true;
			Response.AddHeader("content-disposition", string.Format("attachment;filename={0}.csv", fileName));
			Response.Charset = "";
			Response.ContentType = "text/csv";
			Response.ContentEncoding = System.Text.Encoding.UTF8;

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

		#region Browse Modals
		protected void btnBrowseFields_OnClick(object sender, EventArgs e)
		{
			string html = "<ul>";

			var fieldtemplates = txtFieldTemplates.Value.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

			foreach (var template in fieldtemplates)
			{
				html += "<li data-id='" + template + "' data-name='" + template + "' class='template-heading'>";
				html += string.Format(
					"<a class='browse-expand loaded' onclick='expandNode($(this))>+</a><span>{0}</span><a class='select-all' href='javascript:void(0)' onclick='selectAllFields($(this))'>select all</a>",
					template);
				html += "<ul class='field-list'>";

				var fields = GetAvailableFields(new List<string> { template });

				foreach (var field in fields)
				{
					html += "<li data-name='" + field + "'><a class='field-node ' href='javascript:void(0)' onclick='selectBrowseNode($(this));' ondblclick='selectBrowseNode($(this));addTemplate();' data-id='" + field + "' data-path='" + field + "' data-name='" + field + "'>" + field + "</a></li>";
				}

				html += "</ul>";
				html += "</li>";
			}

			html += "</ul>";

			litBrowseFields.Text = html;

			//litSelectedBrowseFields.Text = GetSelectedItemHtml(inputFields.Value);
			PhBrowseFields.Visible = true;
			PhBrowseModal.Visible = false;
		}

		protected List<string> GetAvailableFields(List<string> fieldtemplates)
		{
			var fields = new List<string>();
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
			return fields;
		}
		#endregion

		protected void btnGetAuthTokenTest_Click(object sender, EventArgs e)
		{
			try
			{
				var token = GetToken();
				litFeedback.Text = token;
			}catch(Exception ex)
			{
				litFeedback.Text = ex.Message;
			}
		}
	}

	#region Classes
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
			get { return new[] { '=', '@', '+', '-' }; }
		}

		public char InjectionEscapeCharacter
		{
			get { return '\t'; }
		}

		/// <summary>
		/// Gets the <see cref="FieldReader"/>.
		/// </summary>
		public IFieldReader FieldReader
		{
			get
			{
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
		public ReadingContext Context
		{
			get
			{
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
		public char[] InjectionCharacters
		{
			get
			{
				return new[] { '=', '@', '+', '-' };
			}
		}

		/// <summary>
		/// Gets or sets the character used to escape a detected injection.
		/// </summary>
		public char InjectionEscapeCharacter
		{
			get
			{
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
		public string QuoteString
		{
			get
			{
				return quoteString;
			}
		}

		/// <summary>
		/// Gets a string representation of two of the currently configured Quote characters.
		/// </summary>
		/// <value>
		/// The new double quote string.
		/// </value>
		public string DoubleQuoteString
		{
			get
			{
				return doubleQuoteString;
			}
		}

		/// <summary>
		/// Gets an array characters that require
		/// the field to be quoted.
		/// </summary>
		public char[] QuoteRequiredChars
		{
			get
			{
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
			get { return encoding; }
			set { encoding = value; }
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
		public int Length
		{
			get
			{
				return position;
			}
		}

		/// <summary>
		/// The total record capacity.
		/// </summary>
		public int Capacity
		{
			get
			{
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

		public IParserConfiguration ParserConfiguration
		{
			get
			{
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
		public string RawRecord
		{
			get
			{
				return RawRecordBuilder.ToString();
			}
		}

		/// <summary>
		/// Gets the field.
		/// </summary>
		public string Field
		{
			get
			{
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
