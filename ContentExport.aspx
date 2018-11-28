<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ContentExport.aspx.cs" Inherits="ContentExportTool.ContentExport" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Content Export Tool</title>
    <style>
        .advanced.open-default .advanced-inner {
            display: block;
        }
         body {
             background-color: rgb(240, 240, 240) !important;
             font-family: 'Open Sans', Arial, sans-serif;
             font-size: 12px;
             color: #131313;
         }

        .header {
            display: block;
            margin-bottom: 7px;
        }

        .notes, .border-notes {
            color: GrayText;
            font-size: 12px;
        }

        .border-notes {
            border-width: 0 1px 1px 1px;
            border-style: solid;
            border-color: #aaa;
            padding: 6px;
            width: 470px;
            display: block;
            margin-bottom: 5px;
        }

        textarea {
            width: 480px;
        }

        .container {
            margin-bottom: 10px;
            font-family: Arial;
            width: 600px;
            padding: 10px;
            font-size: 12px;
        }

        .controls {
            padding: 10px;
        }

        .advanced .advanced-inner {
            display: none;
            margin-top: 10px;
        }

        .advanced .advanced-btn {
            color: rgb(38, 148, 192);
            font-weight: bold;
            padding-bottom: 10px;
            cursor: pointer;
        }

        .advanced .advanced-btn:after {
            border-style: solid;
            border-width: 0.25em 0.25em 0 0;
            content: '';
            display: inline-block;
            height: 0.45em;
            left: 0.15em;
            position: relative;
            vertical-align: top;
            width: 0.45em;
            top: 0;
            transform: rotate(135deg);
            margin-left: 5px;
        }

        .advanced.open a.advanced-btn:after {
            top: 0.3em;
            transform: rotate(-45deg);
        }

        .txtCustomDatabase {
            margin-left: 5px;
        }

        .include-ids {
            color: rgb(38, 148, 192);
            font-size: 14px;
        }

        input[type='text'] {
            width: 500px;
            max-width: 80%;
        }

        input[type='text'].hasDatepicker {
            width: 175px;
        }

        a.clear-btn, .show-hints {
            cursor: pointer;
            color: rgb(38, 148, 192);
            font-size: 11px;
            margin: 10px 0;
            text-transform: capitalize;
            display: block;
        }

        input[type="checkbox"],
        .notes {
            vertical-align: middle;
            margin: 2px;
        }

        .show-hints {
            margin-left: 0;
            display: block;
        }

        .lit-fast-query {
            color: rgb(38, 148, 192);
            font-size: 12px;
        }

        .hints .notes {
            display: block;
            display: none;
            width: 750px;
            max-width: 80%;
        }

        .browse-btn {
            margin-left: 5px;
        }

        .modal.browse-modal {
            z-index: 999;
            position: absolute;
            background: white;
            border: 2px solid rgb(38, 148, 192);
            width: 700px;
            margin-left: 20%;
            height: 60%;
        }

        .selector-box {
            width: 450px;
            overflow: scroll;
            height: 100%;
            float: left;
        }

        .selection-box {
            display: inline-block;
            width: 250px;
            height: 100%;
            position: relative;
        }

        .modal.browse-modal ul {
            list-style: none;
            width: 100%;
            margin-top: 0;
        }

        .modal.browse-modal ul li {
            position: relative;
            left: -20px;
        }

        .modal.browse-modal li ul {
            display: none;
        }

        .modal.browse-modal li.expanded > ul {
            display: block;
        }

        .modal.browse-modal a {
            cursor: pointer;
            text-decoration: none;
            color: black;
        }

        .modal.browse-modal a:hover {
            font-weight: bold;
        }

        .modal.browse-modal .browse-expand {
            color: rgb(38, 148, 192);
            position: absolute;
        }

        .modal.browse-modal .sitecore-node {
            margin-left: 12px !important;
            display: block;
        }

        .main-btns .right {
            float: right;
        }

        .main-btns {
            width: 600px;
            display: inline-block;
            height: auto;
        }

        .main-btns .left {
            float: left;
        }

        .save-settings-box {
            border: 1px solid #aaa;
            background: #eee;
            padding: 5px;
            right: 20px;
            top: 75px;
            position: fixed;
        }

        .save-settings-box input[type="text"] {
            width: 200px;
        }

        .save-settings-close {
            position: absolute;
            right: 2px;
            cursor: pointer;
            top: 2px;
        }

        #btnSaveSettings {
            display: none;
        }

        .error-message {
            color: red;
            font-size: 12px;
            display: none;
        }

        .error-message.server {
            display: block;
        }

        span.save-message {
            color: rgb(38, 148, 192);
            margin-left: 2px;
            display: inline-block;
        }

        .row:not(:last-child) {
            margin-bottom: 20px;
        }

        .btn-clear-all {
            background: none;
            border: none;
            color: rgb(38, 148, 192);
            margin-top: 10px;
            font-size: 14px;
            padding: 0;
            cursor: pointer;
        }

        .selection-box-inner {
            padding: 10px;
        }

        a.btn {
            font-weight: normal !important;
            padding: 1px 6px;
            align-items: flex-start;
            text-align: center;
            cursor: default !important;
            color: buttontext !important;
            background-color: buttonface;
            box-sizing: border-box;
            border-width: 2px;
            border-style: outset;
            border-color: buttonface;
            border-image: initial;
            text-rendering: auto;
            letter-spacing: normal;
            word-spacing: normal;
            text-transform: none;
            text-shadow: none;
            -webkit-appearance: button;
            -webkit-writing-mode: horizontal-tb;
            font: 13.3333px Arial;
        }

        .btn.disabled {
            pointer-events: none;
            color: graytext !important;
        }

        span.selected-node {
            width: 100%;
            word-wrap: break-word;
            display: inline-block;
            font-size: 14px;
        }

        .browse-btns {
            margin-top: 10px;
        }

        .select-box {
            width: 48%;
            height: 100%;
            float: left;
            overflow: auto;
            font-size: 14px;
            position: relative;
        }

        .selector-box {
            position: relative;
            font-size: 14px;
        }

        .selected-box {
            width: 48%;
            height: 100%;
            float: right;
            position: relative;
        }

        .arrows {
            width: 4%;
            height: 100%;
            margin: 0;
            float: left;
            background: #eee;
            font-size: 14px;
        }

        .temp-selected, .temp-selected-remove {
            display: none;
        }

        .modal.browse-modal.templates a.selected, .modal.browse-modal.templates a:hover,
        .modal.browse-modal.fields a.selected, .modal.browse-modal.fields a:hover {
            font-weight: bold;
        }

        .modal.browse-modal.templates a .modal.browse-modal.fields a {
            font-weight: normal;
            font-size: 14px;
        }

        .browse-btns {
            padding: 0 20px 20px 0;
            position: absolute;
            right: 0;
            bottom: 0;
            text-align: right;
            width: 90%;
        }

        #btnBrowseTemplates,
        #btnBrowseFields {
            position: relative;
            top: -13px;
        }

        .modal.browse-modal.templates a {
            font-weight: normal;
        }

        .modal.browse-modal.templates span {
            color: darkgray;
            margin-left: 5px;
        }

        .disabled {
            pointer-events: none;
            color: darkgray !important;
        }

        .advanced-search.disabled {
            pointer-events: initial;
        }

        .advanced-search.disabled input[type="text"], .advanced-search.disabled textarea {
            pointer-events: none;
            background-color: #ddd;
            border: 1px solid #aaa;
        }

        .browse-modal li span {
            margin-left: 10px;
            color: darkgray;
        }

        .modal.browse-modal.fields a {
            font-weight: normal;
        }

        .modal.browse-modal a.select-all {
            font-size: 12px;
            margin-left: 5px;
            color: rgb(38, 148, 192);
            cursor: pointer;
        }

        ul.selected-box-list a {
            font-size: 14px;
        }

        ul.selected-box-list {
            max-height: 90%;
            overflow-y: auto;
            width: 100%;
            padding-left: 0;
            margin: 0;
        }

        .modal.browse-modal ul.selected-box-list li {
            left: 0;
            padding-left: 10px;
        }

        .arrows .btn {
            position: relative;
            top: 150px;
            margin-bottom: 10px;
        }

        input.field-search {
            width: 94%;
            display: inline-block;
            margin-bottom: 10px;
            max-width: none;
            padding: 4px 16px 2px 5px;
            border: none;
            border-bottom: 1px solid #ccc;
        }

        ::-webkit-input-placeholder { /* Chrome/Opera/Safari */
            font-style: italic;
        }

        ::-moz-placeholder { /* Firefox 19+ */
            font-style: italic;
        }

        :-ms-input-placeholder { /* IE 10+ */
            font-style: italic;
        }

        :-moz-placeholder { /* Firefox 18- */
            font-style: italic;
        }

        a.clear-search {
            position: absolute;
            right: 2px;
            top: 2px;
            color: darkgray !important;
        }

        li.hidden {
            display: none;
        }

        .hidden {
            display: none;
        }

        .clear-selections {
            float: left;
        }

        span.api-message {
            margin-bottom: 10px;
            display: block;
            font-size: 16px;
        }

        .modal span.api-message {
            margin: 0;
            border-bottom: 1px solid #ccc;
            padding: 4px;
            font-size: 14px;
        }

        .modal span.api-message a {
            color: blue;
            text-decoration: underline;
        }

        .modal span.api-message a:hover {
            font-weight: normal;
        }

        .loader {
            border: 16px solid #f3f3f3; /* Light grey */
            border-top: 16px solid #3498db; /* Blue */
            border-radius: 50%;
            width: 40px;
            height: 40px;
            animation: spin 2s linear infinite;
        }

        .loading-modal {
            display: none;
            width: 100%;
            height: 100%;
            position: fixed;
            background: rgba(0,0,0,.2);
            top: 0;
            left: 0;
            z-index: 999;
        }

        .loading-box {
            position: absolute;
            top: 40%;
            padding: 40px;
            left: 42%;
            border-radius: 10px;
        }

        @keyframes spin {
            0% {
                transform: rotate(0deg);
            }

            100% {
                transform: rotate(360deg);
            }
        }

        .advanced-search {
            background: #eee;
            padding: 20px;
            border: 1px solid #ccc;
        }

        .scMessageBar.scWarning,
        .scMessageBar.scWarning a {
            background-color: #FCE99C;
            color: #897B2F;
        }

        .scMessageBar {
            font-size: 12px;
            display: -ms-flexbox;
            display: flex;
            align-items: center;
        }

        .scMessageBar.scWarning .scMessageBarIcon {
            background-image: url(/sitecore/shell/themes/standard/Images/warning_yellow.png);
            background-color: #E0B406;
        }

        .scMessageBar .scMessageBarIcon {
            background-repeat: no-repeat;
            background-position: center;
            background-size: 32px;
            min-width: 50px;
            min-height: 50px;
            align-self: stretch;
        }

        .scMessageBar .scMessageBarTextContainer {
            padding: 11px 14px;
        }

        .scMessageBar .scMessageBarTextContainer .scMessageBarTitle {
            display: block;
            font-weight: 600;
        }
    </style>
    <link rel="stylesheet" href="//code.jquery.com/ui/1.12.1/themes/base/jquery-ui.css" />
    <script src="https://code.jquery.com/jquery-2.2.4.min.js"></script>
    <script src="https://code.jquery.com/ui/1.11.3/jquery-ui.min.js"></script>
    <script src="ContentExportScripts.js"></script>
</head>
<body>
    <asp:PlaceHolder runat="server" ID="phOverwriteScript" Visible="False">
        <script>
            $(document).ready(function () {
                var overwrite = confirm("There are already settings saved with this name. Do you want to overwrite?");
                if (overwrite) {
                    $(".btn-overwrite").click();
                }
            });
        </script>
    </asp:PlaceHolder>
    <form id="form1" runat="server">
        <div class="loading-modal">
            <div class="loading-box">
                <div class="loader"></div>
            </div>
        </div>
        <input runat="server" id="txtDownloadToken" style="display: none;" />

        <div>
            <asp:PlaceHolder runat="server" ID="PhApiMessage" Visible="False">
                <div class="scMessageBar scWarning">
                    <div class="scMessageBarIcon"></div>
                    <div class="scMessageBarTextContainer">
                        <div class="scMessageBarTitle">The Sitecore Item API is not enabled in your site.</div>
                        <div class="scMessageBarText">To improve browsing performance, turn on the  <a target="_blank" href="https://sdn.sitecore.net/upload/sdn5/modules/sitecore%20item%20web%20api/sitecore_item_web_api_developer_guide_sc65-66-usletter.pdf#page=12">Sitecore Item API</a></div>
                    </div>
                </div>
            </asp:PlaceHolder>
            <div class="container">
                <asp:Literal runat="server" ID="litFeedback"></asp:Literal>
            </div>
            <div class="controls">
                <div class="save-settings-box">
                    <div class="row">
                        <span class="header">Enter a name to save: </span>
                        <input runat="server" id="txtSaveSettingsName" />
                        <input type="button" class="save-btn-decoy" value="Save Settings" />
                        <asp:Button runat="server" ID="btnSaveSettings" OnClick="btnSaveSettings_OnClick" Text="Save Settings" /><span class="save-message">
                            <asp:Literal runat="server" ID="litSavedMessage"></asp:Literal></span>
                        <asp:Button runat="server" ID="btnOverWriteSettings" OnClick="btnOverWriteSettings_OnClick" CssClass="hidden btn-overwrite" />

                        <span class="error-message">You must enter a name for this configuration<br />
                        </span>
                    </div>
                    <div class="row">
                        <span class="header">Saved settings: </span>
                        <asp:DropDownList runat="server" ID="ddSavedSettings" AutoPostBack="True" OnSelectedIndexChanged="ddSavedSettings_OnSelectedIndexChanged" />
                        <a runat="server" visible="False" id="btnDeletePrompt" class="btn" onclick="confirmDelete()">Delete</a>
                        <asp:Button runat="server" ID="btnDeleteSavedSetting" OnClick="btnDeleteSavedSetting_OnClick" CssClass="hidden btn-delete" /><br />
                    </div>
                </div>
                <div class="container">

                    <asp:PlaceHolder runat="server" ID="PhBrowseTree">
                        <div class="modal browse-modal">
                            <div class="selector-box left">
                                <input class="field-search" type="text" placeholder="search" onkeyup="browseSearch($(this))" />
                                <a class="clear-search" href="javascript:void(0)" onclick="clearSearch($(this))">X</a>
                                <asp:Literal runat="server" ID="litSitecoreContentTree"></asp:Literal>
                            </div>
                            <div class="selection-box">
                                <div class="selection-box-inner">
                                    <span class="header">Selected node:</span><br />
                                    <span class="selected-node">(No node selected)</span>
                                    <div class="browse-btns">
                                        <a href="javascript:void(0)" class="btn disabled select-node-btn" onclick="confirmSelection();">Select</a>
                                        <a class="btn close-modal" onclick="closeTreeBox()">Cancel</a>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </asp:PlaceHolder>

                    <asp:PlaceHolder runat="server" ID="PhBrowseTemplates">
                        <div class="modal browse-modal templates">
                            <div class="select-box left" id="templateLinks">
                                <input class="field-search" type="text" placeholder="search" onkeyup="browseSearch($(this))" />
                                <a class="clear-search" href="javascript:void(0)" onclick="clearSearch($(this))">X</a>
                                <asp:Literal runat="server" ID="litBrowseTemplates"></asp:Literal>
                            </div>
                            <div class="arrows">
                                <a class="btn" onclick="addTemplate()">&raquo;</a>
                                <a class="btn" onclick="removeTemplate()">&laquo;</a>
                            </div>
                            <div class="selected-box">
                                <span class="temp-selected"></span>
                                <span class="temp-selected-remove"></span>
                                <ul class="selected-box-list">
                                </ul>
                                <div class="browse-btns">
                                    <a href="javascript:void" class="btn clear-selections" onclick="clearModalSelections();">Clear</a>
                                    <a href="javascript:void(0)" class="btn disabled select-node-btn" onclick="confirmTemplateSelection();">Select</a>
                                    <a class="btn close-modal" onclick="closeTemplatesModal()">Cancel</a>
                                </div>
                            </div>
                        </div>
                    </asp:PlaceHolder>

                    <asp:PlaceHolder runat="server" ID="PhBrowseFields">
                        <div class="modal browse-modal fields">
                            <div class="select-box left">
                                <input class="field-search" type="text" placeholder="search" onkeyup="browseSearch($(this))" />
                                <a class="clear-search" href="javascript:void(0)" onclick="clearSearch($(this))">X</a>
                                <asp:Literal runat="server" ID="litBrowseFields"></asp:Literal>
                            </div>
                            <div class="arrows">
                                <a class="btn" onclick="addTemplate()">&raquo;</a>
                                <a class="btn" onclick="removeTemplate()">&laquo;</a>
                            </div>
                            <div class="selected-box">
                                <span class="temp-selected"></span>
                                <span class="temp-selected-remove"></span>
                                <ul class="selected-box-list">
                                </ul>
                                <div class="browse-btns">
                                    <a href="javascript:void" class="btn clear-selections" onclick="clearModalSelections();">Clear</a>
                                    <a href="javascript:void(0)" class="btn disabled select-node-btn" onclick="confirmFieldSelection();">Select</a>
                                    <a class="btn close-modal" onclick="closeFieldModal()">Cancel</a>
                                </div>
                            </div>
                        </div>
                    </asp:PlaceHolder>

                    <div class="row">
                        <asp:Button runat="server" ID="btnRunExport" OnClick="btnRunExport_OnClick" Text="Run Export" /><br />
                        <asp:Button runat="server" ID="btnClearAll" Text="Clear All" OnClick="btnClearAll_OnClick" CssClass="btn-clear-all" />
                    </div>
                    <div class="row">
                        <span class="header">Database</span>
                        <asp:DropDownList runat="server" ID="ddDatabase" CssClass="ddDatabase" />
                        <input runat="server" class="txtCustomDatabase" id="txtCustomDatabase" style="display: none" />
                        <span class="notes">Select database. Defaults to web</span>
                    </div>
                    <div class="row">
                        <span class="header">Include ID</span>
                        <asp:CheckBox runat="server" ID="chkIncludeIds" />
                        <span class="notes">Check this box to include item IDs (guid) in the exported file.</span>
                    </div>
                    <div class="row">
                        <span class="header">Include Name</span>
                        <asp:CheckBox runat="server" ID="chkIncludeName" />
                        <span class="notes">Check this box to include the item name</span>
                    </div>
                    <div class="row">
                        <span class="header">Start Item</span>
                        <a class="clear-btn" data-id="inputStartitem">clear</a>
                        <input runat="server" id="inputStartitem" /><asp:Button runat="server" ID="btnBrowse" OnClick="btnBrowse_OnClick" CssClass="browse-btn" Text="Browse" />
                        <span class="border-notes">Enter the path or ID of the starting node, or use Browse* to select.<br />
                            Only content beneath and including this node will be exported. If field is left blank, the starting node will be /sitecore/content.<br />
                            *Browse might take a while to load</span>
                    </div>
                    <div class="row">
                        <span>OR</span>
                    </div>
                    <div class="row">
                        <span class="header">Fast Query</span>
                        <a class="clear-btn" id="clear-fast-query" data-id="txtFastQuery">clear</a>
                        <input runat="server" id="txtFastQuery" />
                        <asp:Button runat="server" ID="btnTestFastQuery" OnClick="btnTestFastQuery_OnClick" Text="Test" />
                        <span class="border-notes">Enter a fast query to run a filtered export. You can use the Templates box as well.<br />
                            Example: fast:/sitecore/content/Home//*[@__Updated >= '20140610' and @__Updated <'20140611']</span><br />
                        <span class="lit-fast-query">
                            <asp:Literal runat="server" ID="litFastQueryTest"></asp:Literal>
                        </span>
                    </div>
                    <div class="row">
                        <span class="header">Templates</span>
                        <a class="clear-btn" data-id="inputTemplates">clear</a>
                        <textarea runat="server" id="inputTemplates" cols="60" row="5"></textarea><asp:Button runat="server" ID="btnBrowseTemplates" OnClick="btnBrowseTemplates_OnClick" CssClass="browse-btn" Text="Browse" />
                        <span class="border-notes">Enter template names and/or IDs separated by commas, or use Browse to select.
                            <br />
                            Items will only be exported if their template is in this list. If this field is left blank, all templates will be included.</span><br />
                        <br />
                        <div class="hints">
                            <a class="show-hints">Hints</a>
                            <span class="notes">Example: Standard Page, {12345678-901-2345-6789-012345678901}
                            </span>
                        </div>
                    </div>
                    <div class="row">
                        <span class="header">Include Template Name</span>
                        <asp:CheckBox runat="server" ID="chkIncludeTemplate" />
                        <span class="notes">Check this box to include the template name with each item</span>
                    </div>
                    <div class="row">
                        <span class="header">Include Templates that Inherit Selected Templates</span>
                        <asp:CheckBox runat="server" ID="chkIncludeInheritance" />
                        <span class="notes">Check this box toinclude any templates that inherit selected templates</span>
                    </div>
                    <div class="row">
                        <span class="header">Fields</span>
                        <a class="clear-btn" data-id="inputFields">clear</a>
                        <textarea runat="server" id="inputFields" cols="60" row="5"></textarea><asp:Button runat="server" ID="btnBrowseFields" OnClick="btnBrowseFields_OnClick" CssClass="browse-btn" Text="Browse" />
                        <span class="border-notes">Enter field names or IDs separated by commas, or use Browse to select fields.</span>
                        <asp:CheckBox runat="server" ID="chkAllFields" />
                        <span class="notes"><b style="color: black">All Fields</b> - This will export the values of <b>all fields</b> of every included item. <b>This may take a while.</b></span><br />
                        <asp:CheckBox runat="server" ID="chkComponentFields" />
                        <span class="notes"><b style="color: black">Include Component Fields</b> - This will export the values of fields that are on a page's component items as well as on the page item itself</span>
                    </div>

                    <div class="advanced">
                        <a class="advanced-btn">Advanced Options</a>
                        <div class="advanced-inner">
                            <div class="row advanced-search">
                                <span class="header"><b>Advanced Search:</b></span>
                                <input runat="server" id="txtAdvancedSearch" /><asp:Button runat="server" ID="btnAdvancedSearch" OnClick="btnAdvancedSearch_OnClick" Text="Go" />
                                <span class="border-notes">Export all items that contain the search text in a field. 
                                    <br />
                                    By default, this will check ALL fields on each item; if fields are specified in the Fields box, only those fields will be searched
                                    <br />
                                    Advanced search works with the Start Item, Templates, and Fields boxes
                                </span>
                            </div>
                            <div class="row advanced-search" style="display: none;">
                                <span class="header" style="display: inline-block"><b>Advanced Item Selection</b></span>
                                <asp:CheckBox runat="server" ID="chkAdvancedSelectionOff" /><span>Off</span>
                                <span class="header">Get items that are linked in the following fields: </span>
                                <a class="clear-section-btn clear-btn">clear</a>
                                <textarea runat="server" id="txtAdvFields" cols="60" row="5"></textarea><br />
                                <span class="border-notes">Use this field with the Start Item/Fast Query/Multiple Start Items fields, the Templates field, and/or any other filters. Rather than exporting the items selected by those filters, this will export the items <i>linked</i> in those items in the fields specified (or in all fields, if the checkbox below is checked)</span>
                                <span class="header">Get all linked items</span>
                                <asp:CheckBox runat="server" ID="chkAdvAllLinkedItems" />
                            </div>
                            <div class="row advanced-search">
                                <div class="row">
                                    <span class="header"><b>Created Date Range</b></span>
                                    <a class="clear-section-btn clear-btn">Clear</a>
                                    Created between
                                    <input type="text" runat="server" id="txtStartDateCr" autocomplete="off" />
                                    and
                                    <input type="text" runat="server" id="txtEndDateCr" autocomplete="off" />
                                    <span class="border-notes">Only return items created between the selected time span</span>
                                </div>
                                <div class="row">
                                    <input name="radDateRange" type="radio" runat="server" id="radDateRangeOr" /><span><b>OR</b></span>
                                    <input name="radDateRange" type="radio" runat="server" id="radDateRangeAnd" /><span><b>AND</b></span>
                                </div>
                                <div class="row">
                                    <span class="header"><b>Published Date Range</b></span>
                                    <a class="clear-section-btn clear-btn">Clear</a>
                                    Updated between
                                    <input type="text" runat="server" id="txtStartDatePb" autocomplete="off" />
                                    and
                                    <input type="text" runat="server" id="txtEndDatePu" autocomplete="off" />
                                    <span class="border-notes">Only return items last published between the selected time span</span>
                                </div>
                            </div>
                            <div class="row">
                                <span class="header">Multiple Start Items</span>
                                <a class="clear-btn" data-id="inputMultiStartItem">clear</a>
                                <textarea cols="60" row="5" runat="server" id="inputMultiStartItem"></textarea>
                                <span class="border-notes">Enter multiple start paths or item IDs separated by comma to include items under separate starting nodes; can be used in tandem with Start Item/ fast query</span><br />
                            </div>
                            <div class="row">
                                <span class="header">Only include items with layout</span>
                                <asp:CheckBox runat="server" ID="chkItemsWithLayout" />
                                <span class="notes">Check this box to only include items that have a layout, i.e. template pages and not components</span>
                            </div>
                            <div class="row">
                                <span class="header">Include linked item IDs </span>
                                <asp:CheckBox runat="server" ID="chkIncludeLinkedIds" />
                                <span class="notes">(images, links, droplists, multilists)</span>
                            </div>
                            <div class="row">
                                <span class="header">Include raw HTML </span>
                                <asp:CheckBox runat="server" ID="chkIncludeRawHtml" /><span class="notes">(images and links)</span>
                            </div>
                            <div class="row">
                                <span class="header">Referrers</span>
                                <asp:CheckBox runat="server" ID="chkReferrers" />
                                <span class="notes">Include the paths of all items that refer to each item</span>
                            </div>
                            <div class="row">
                                <span class="header">Date Created</span>
                                <asp:CheckBox runat="server" ID="chkDateCreated" />
                            </div>
                            <div class="row">
                                <span class="header">Created By</span>
                                <asp:CheckBox runat="server" ID="chkCreatedBy" />
                            </div>
                            <div class="row">
                                <span class="header">Date Modified</span>
                                <asp:CheckBox runat="server" ID="chkDateModified" />
                            </div>
                            <div class="row">
                                <span class="header">Modified By</span>
                                <asp:CheckBox runat="server" ID="chkModifiedBy" />
                            </div>
                            <div class="row">
                                <span class="header">Never Publish?</span>
                                <asp:CheckBox runat="server" ID="chkNeverPublish" />
                            </div>
                            <div class="row">
                                <span class="header">Workflow</span>
                                <asp:CheckBox runat="server" CssClass="workflowBox" ID="chkWorkflowName" />
                            </div>
                            <div class="row">
                                <span class="header">Workflow State</span>
                                <asp:CheckBox runat="server" CssClass="workflowBox" ID="chkWorkflowState" />
                                <span class="notes">Workflow options require the database to be set to master</span>
                            </div>
                            <div class="row">
                                <span class="header">Language</span>
                                <%--<asp:ListBox runat="server" ID="lstLanguages" SelectionMode="multiple" Width="200">
                                </asp:ListBox>--%>
                                <asp:DropDownList runat="server" ID="ddLanguages" />
                            </div>
                            <div class="row">
                                <span class="header">Get All Language Versions</span>
                                <asp:CheckBox runat="server" ID="chkAllLanguages" />
                                <span class="notes">This will get the selected field values for all languages that each item has an existing version for</span>
                            </div>
                            <div class="row">
                                <span class="header">Download File Name</span><br />
                                <input runat="server" id="txtFileName" />
                            </div>
                            <div class="row">
                                <asp:Button runat="server" ID="btnRunExportDupe" OnClick="btnRunExport_OnClick" Text="Run Export" />
                            </div>
                        </div>
                    </div>
                    <br />
                    <br />
                    <div class="advanced open open-default">
                        <a class="advanced-btn">Content Import</a>
                        <div class="advanced-inner">
                            <div class="row advanced-search">
                                <span style="color:red" class="uploadResponse"><asp:Literal runat="server" id="litUploadResponse"></asp:Literal></span>
                                <%--                                <span class="header"><b>Advanced Search:</b></span>--%>
                                <asp:FileUpload runat="server" ID="btnFileUpload" Text="Upload File" />
                                <span class="" style="display: block; margin-top: 10px;">
                                    <b>Important:</b>To create new items CSV must include the following fields: Item Path, Template, Name. In the Item Path field, put in the path of the parent item.
                                    <br/>
                                    To edit existing items, CSV must include Item Path
                                    <br/>
                                    By default, the import will NOT overwrite exising items, but will only create new items.
                                    <br />
                                    To overwrite existing items, check off the radio button below.
                                    <br />
                                    
                                    <br />
                                    <br />
                                    <b>Download CSV Template: </b>
                                    <asp:Button runat="server" ID="btnDownloadCSVTemplate" Text="Download Template" OnClick="btnDownloadCSVTemplate_OnClick"/>
                                    <br/><br/><br/>
                                </span>

                                <div class="row">
                                    <asp:Button runat="server" ID="btnCreateItems" OnClick="btnCreateItems_OnClick" Text="Create Items" />
                                    <asp:Button runat="server" ID="btnEditItems" OnClick="btnEditItems_OnClick" Text="Edit Items" />
                                </div>
                                
                                <h3>Tips:</h3>
                                <ul>
                                    <li>Add a column for every field that you want to add content to (e.g. replace Field1 in the example template with a valid field name)</li>
                                    <li>If a template name is not unique, use the template ID instead</li>
                                    <li>If you are modifying existing content, for best results run an export on that content first, make  your changes in the downloaded file and re-upload that file to import.</li>
                                    <li> To <b>edit</b> content, Item Path must be the path of the item you with to edit.<br/>
                                        
                                    </li>
                                    <li>To <b>create</b> content, the Item Path must be the path of the parent item you wish to create the new item under (parent item must already exist);
                                        <ul>
                                            <li>
                                                Make sure to include Name and Template when creating items
                                            </li>
                                            <li>
                                                Name and Template are not necessary for editing items
                                            </li>
                                        </ul></li>
                                    <li>Note: The import function currently supports string, image, and link fields. It does not support more complex field types, such as droplists or multilists.</li>
                                </ul>

                            </div>

                        </div>
                    </div>

                </div>
            </div>
        </div>
    </form>
</body>
</html>
