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
            padding: 10px;
            font-size: 12px;
            width: calc(100% - 412px);
            max-width: 70%;
            min-width:200px;
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

        #ddLanguages {
            width: 100%;
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
            position: fixed;
            top: 20%;
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
            width: auto;
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
            width: 312px;
            max-width: 25%;
        }

        .fixed-export-btn {
            position: fixed;
            width: 302px;
            max-width: 25%;
            right: 20px;
            top: 245px;
            padding: 10px;
            border: 1px solid #aaa;
            background: #eee;
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
            height: 95%;
            float: left;
            overflow: auto;
            font-size: 14px;
            position: relative;
        }

        .selector-box {
            position: relative;
            font-size: 14px;
        }

            .selector-box.left, .select-box.left {
                padding-top: 10px;
                white-space: nowrap;
                overflow-x: auto;
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

        .modal.browse-modal a.selected, .modal.browse-moal a:hover,
        .modal.browse-modal.fields a.selected, .modal.browse-modal.fields a:hover {
            font-weight: bold;
        }

        .modal.browse-moal a .modal.browse-modal.fields a {
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

        .modal.browse-moal a {
            font-weight: normal;
        }

        .modal.browse-moal span {
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
            padding-top: 10px;
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

        .advanced-search, .inner-section {
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

        .select-box img.scSpinner {
            position: absolute;
            top: 3px;
            background: white;
            left: -2px;
        }

        .btnSampleLink {
            cursor: pointer;
            background: none;
            border: none;
            color: rgb(38, 148, 192);
            padding-left: 0;
        }

        #singleTemplate .content {
            height: 90%;
            overflow: scroll;
            overflow-x: hidden;
        }

        #singleTemplate .buttons {
            float: right;
            padding-right: 20px;
        }

        input[type="checkbox"] + span.notes {
            display: inline-block;
            width: 88%;
            margin-bottom: 5px;
        }

        input[type="checkbox"] {
            display: inline-block;
            vertical-align: top;
        }

        select#ddSavedSettings {
            min-width: 60%;
            max-width: 75%;
        }

        a.navButton {
            display: block;
            padding-top: 8px;
            font-size: 14px;
            text-decoration: none;
            cursor: pointer;
        }

        input#btnDownloadRenderingParamsSample {
            background: none;
            border: none;
            text-decoration: underline;
            cursor: pointer;
            color: rgb(38, 148, 192);
            padding: 0;
            font-size: 12px;
        }

        span.uploadResponse {
            display: block;
            margin-bottom: 4px;
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
    <asp:PlaceHolder runat="server" ID="phDeleteScript" Visible="False">
        <script>
            $(document).ready(function () {
                alert("You cannot delete another user's saved settings");
            });
        </script>
    </asp:PlaceHolder>
    <asp:PlaceHolder runat="server" ID="phScrollToImport" Visible="False">
        <script>
            $(document).ready(function () {
                location.href = "#contentImport";
            });
        </script>
    </asp:PlaceHolder>
    <asp:PlaceHolder runat="server" ID="phScrollToRenderingImport" Visible="False">
        <script>
            $(document).ready(function () {
                location.href = "#renderingParamsImport";
            });
        </script>
    </asp:PlaceHolder>
    <form id="form1" runat="server">
        <div class="loading-modal">
            <div class="loading-box">
                <img class="scSpinner" src="/sitecore/shell/themes/standard/Images/ProgressIndicator/sc-spinner32.gif" border="0" alt="" width="40px" />
            </div>
        </div>
        <input runat="server" id="txtDownloadToken" style="display: none;" />


        <%--  <input runat="server" id="txtStoreContentTree"/>
        <input runat="server" id="txtStoreTemplatesTree"/>
        <input runat="server" id="txtStoreFieldsTree"/>--%>

        <div>
            <div class="container feedback">
                <asp:Literal runat="server" ID="litFeedback"></asp:Literal>
            </div>
            <div class="controls">
                <div class="save-settings-box">
                    <div class="row">
                        <span class="header">Enter a name to save: </span>
                        <input runat="server" id="txtSaveSettingsName" />
                        <input type="button" class="save-btn-decoy" value="Save Settings" />
                        <asp:Button class="spinner-btn" runat="server" ID="btnSaveSettings" OnClick="btnSaveSettings_OnClick" Text="Save Settings" /><span class="save-message">
                            <asp:Literal runat="server" ID="litSavedMessage"></asp:Literal></span>
                        <asp:Button runat="server" ID="btnOverWriteSettings" OnClick="btnOverWriteSettings_OnClick" CssClass="hidden btn-overwrite" />

                        <span class="error-message">You must enter a name for this configuration<br />
                        </span>
                    </div>
                    <div class="row">
                        <span class="header">Saved settings: </span>
                        <asp:DropDownList runat="server" ID="ddSavedSettings" AutoPostBack="True" OnSelectedIndexChanged="ddSavedSettings_OnSelectedIndexChanged" />
                        <a runat="server" visible="False" id="btnDeletePrompt" class="btn" onclick="confirmDelete()">Delete</a>
                        <asp:Button class="spinner-btn" runat="server" ID="btnDeleteSavedSetting" OnClick="btnDeleteSavedSetting_OnClick" CssClass="hidden btn-delete" /><br />
                        <asp:CheckBox runat="server" AutoPostBack="True" OnCheckedChanged="chkAllUserSettings_OnCheckedChanged" ID="chkAllUserSettings" /><span class="notes">Show settings for all users</span>
                    </div>
                </div>

                <div class="fixed-export-btn">
                    <span class="header"><b>Quick Links</b></span>
                    <div class="row">
                        <asp:Button class="spinner-btn" runat="server" ID="btnRunExportDupe" OnClick="btnRunExport_OnClick" Text="Run Content Export" />
                        <a class="navButton" href="#divFilters">Filters</a>
                        <a class="navButton" href="#divExportData">Fields and Data</a>
                        <a class="navButton" href="#divAudits">Special Audits & Search</a>
                        <a class="navButton" href="#packageExport">Package Export</a>
                        <a class="navButton" href="#contentImport">Content Import</a>
                        <a class="navButton" href="#renderingParamsImport">Rendering Parameters Import</a>
                        <a class="navButton" href="javascript:void(0)" onclick="window.scrollTo(0,0);">Back to Top</a>
                    </div>
                </div>
                <div class="container">

                    <asp:PlaceHolder runat="server" ID="PhBrowseModal">
                        <div class="" runat="server" id="divBrowseContainer">
                            <div class="select-box left" id="templateLinks">
                                <asp:Literal runat="server" ID="litBrowseTree"></asp:Literal>
                            </div>
                            <div class="arrows">
                                <a class="btn" onclick="addTemplate()">&raquo;</a>
                                <a class="btn" onclick="removeTemplate()">&laquo;</a>
                            </div>
                            <div class="selected-box templates-and-content">
                                <span class="temp-selected"></span>
                                <span class="temp-selected-remove"></span>
                                <ul class="selected-box-list">
                                    <asp:Literal runat="server" ID="litSelectedBrowseItems"></asp:Literal>
                                </ul>
                                <div class="browse-btns">
                                    <a href="javascript:void" class="btn clear-selections" onclick="clearModalSelections();">Clear</a>
                                    <a href="javascript:void(0)" class="btn select-node-btn" onclick="confirmBrowseSelection();">Select</a>
                                    <a class="btn close-modal" onclick="closeTemplatesModal()">Cancel</a>
                                </div>
                            </div>
                        </div>
                    </asp:PlaceHolder>

                    <asp:PlaceHolder runat="server" ID="PhBrowseFields">
                        <div class="modal browse-modal fields">
                            <div class="select-box left">
                                <asp:Literal runat="server" ID="litBrowseFields"></asp:Literal>
                            </div>
                            <div class="arrows">
                                <a class="btn" onclick="addTemplate()">&raquo;</a>
                                <a class="btn" onclick="removeTemplate()">&laquo;</a>
                            </div>
                            <div class="selected-box fields">
                                <span class="temp-selected"></span>
                                <span class="temp-selected-remove"></span>
                                <ul class="selected-box-list">
                                    <asp:Literal runat="server" ID="litSelectedBrowseFields"></asp:Literal>
                                </ul>
                                <div class="browse-btns">
                                    <a href="javascript:void" class="btn clear-selections" onclick="clearModalSelections();">Clear</a>
                                    <a href="javascript:void(0)" class="btn select-node-btn" onclick="confirmFieldSelection();">Select</a>
                                    <a class="btn close-modal" onclick="closeFieldModal()">Cancel</a>
                                </div>
                            </div>
                        </div>
                    </asp:PlaceHolder>

                    <div class="row">
                        <asp:Button class="spinner-btn" runat="server" ID="btnRunExport" OnClick="btnRunExport_OnClick" Text="Run Content Export" /><br />
                        <asp:Button runat="server" ID="btnClearAll" Text="Clear All" OnClick="btnClearAll_OnClick" CssClass="btn-clear-all" />
                    </div>

                    <div class="row">
                        <span class="header">Database</span>
                        <asp:DropDownList runat="server" ID="ddDatabase" CssClass="ddDatabase" />
                        <input runat="server" class="txtCustomDatabase" id="txtCustomDatabase" style="display: none" />
                        <span class="notes">Select database</span>
                    </div>

                    <div class="row">
                        <span class="header">Start Item(s)</span>
                        <a class="clear-btn" data-id="inputStartitem">clear</a>
                        <textarea runat="server" id="inputStartitem" /><asp:Button runat="server" ID="btnBrowse" OnClick="btnBrowse_OnClick" CssClass="browse-btn" Text="Browse" />
                        <span class="border-notes">Enter the path or ID of each starting node, or use Browse to select.<br />
                            Only content beneath and including this node will be exported. If field is left blank, the starting node will be /sitecore/content.</span>

                        <asp:CheckBox runat="server" ID="chkNoChildren" /><span class="notes"><b style="color: black">No children</b> (only include the items selected above)</span><br />
                        <br />

                        <div class="advanced open open-default" runat="server" id="divFilters">
                            <a class="advanced-btn">Filters</a>
                            <div class="advanced-inner">
                                <div class="inner-section">
                                    <h3>Filters</h3>

                                    <div class="inner-section">
                                        <div class="row">
                                            <span class="header"><b>Fast Query</b></span>
                                            <a class="clear-btn" id="clear-fast-query" data-id="txtFastQuery">clear</a>
                                            <input runat="server" id="txtFastQuery" />
                                            <asp:Button runat="server" ID="btnTestFastQuery" OnClick="btnTestFastQuery_OnClick" Text="Test" />
                                            <span class="border-notes"><b>Fast Query will override the Start Item selection</b><br>
                                                Enter a fast query to run a filtered export. You can use the Templates filter in tandem with Fast Query.
                                                <br />
                                                Example: fast:/sitecore/content/Home//*[@__Updated >= '20180101' and @__Updated <= '20181231']</span><br />
                                            <span class="lit-fast-query">
                                                <asp:Literal runat="server" ID="litFastQueryTest"></asp:Literal>
                                            </span>
                                        </div>
                                    </div>
                                    <br />

                                    <div class="inner-section">
                                        <div class="row">
                                            <span class="header"><b>Templates</b></span>
                                            <a class="clear-btn" data-id="inputTemplates">clear</a>
                                            <textarea runat="server" id="inputTemplates" cols="60" row="5"></textarea><asp:Button runat="server" ID="btnBrowseTemplates" OnClick="btnBrowseTemplates_OnClick" CssClass="browse-btn" Text="Browse" />
                                            <span class="border-notes">Enter template names and/or IDs separated by commas, or use Browse to select.
                                        <br />
                                                Items will only be exported if their template is in this list. If this field is left blank, all templates will be included.</span>
                                            <div class="hints">
                                                <a class="show-hints">Hints</a>
                                                <span class="notes">Example: Standard Page, {12345678-901-2345-6789-012345678901}
                                                </span>
                                            </div>
                                            <asp:CheckBox runat="server" ID="chkIncludeInheritance" />
                                            <span class="notes"><b style="color: black">Include Inheritors</b> - Include any templates that inherit selected templates</span>
                                        </div>
                                        <div class="row">
                                            <span class="header"><b>Exclude Templates</b></span>
                                            <a class="clear-btn" data-id="inputExcludeTemplates">clear</a>
                                            <textarea runat="server" id="inputExcludeTemplates" cols="60" row="5"></textarea><asp:Button runat="server" ID="btnBrowseExcludeTemplates" OnClick="btnBrowseExcludeTemplates_OnClick" CssClass="browse-btn" Text="Browse" />
                                            <span class="border-notes">Enter template names and/or IDs separated by commas, or use Browse to select.
                                       
                                                <br />
                                                Items in this list will be <b>excluded</b> from the report (this is only needed if the Templates field is empty)</span>
                                        </div>
                                        <div class="row">
                                            <span class="header"><b>Only include items with layout</b></span>
                                            <asp:CheckBox runat="server" ID="chkItemsWithLayout" />
                                            <span class="notes">Only export items that have a layout, i.e. template pages and not components</span>
                                        </div>
                                    </div>
                                    <br />



                                    <div class="inner-section">
                                        <div class="row">
                                            <span class="header"><b>Created Date Range</b></span>
                                            <a class="clear-section-btn clear-btn">Clear</a>
                                            Created between
                                    <input type="text" runat="server" id="txtStartDateCr" autocomplete="off" />
                                            and
                                    <input type="text" runat="server" id="txtEndDateCr" autocomplete="off" />
                                            <span class="border-notes">Only export items created between the selected time span</span>
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
                                            <span class="border-notes">Only export items last published between the selected time span</span>
                                        </div>
                                    </div>
                                    <br />

                                    <div class="inner-section">
                                        <div class="header"><b>Sitecore User</b></div>
                                        <a class="clear-section-btn clear-btn">Clear</a>
                                        <div class="row">
                                            <span class="header">Created By</span>
                                            <input runat="server" id="txtCreatedByFilter" /><br />
                                            <span class="notes">Only export items created by the given username (not case sensitive, but exact match required; enter as "Sitecore/username" or "username")
                                        <br />
                                                Separate multiple usernames by comma ("username1, username2")
                                            </span>
                                        </div>

                                        <div class="row">
                                            <span class="header">Modified By</span>
                                            <input runat="server" id="txtModifiedByFilter" /><br />
                                            <span class="notes">Only export items last published by the given username (not case sensitive, but exact match required; enter as "Sitecore/username" or "username")
                                         <br />
                                                Separate multiple usernames by comma ("username1, username2")
                                            </span>
                                        </div>
                                    </div>
                                    <br />

                                    <div class="inner-section">
                                        <h3>Additional Options</h3>
                                        <div class="row" style="width: 25%; display: inline-block">
                                            <span class="header">Language</span>
                                            <%--<asp:ListBox runat="server" ID="lstLanguages" SelectionMode="multiple" Width="200">
                                </asp:ListBox>--%>
                                            <asp:DropDownList runat="server" ID="ddLanguages" />
                                        </div>
                                        <span style="padding: 0 20px;">OR</span>

                                        <div class="row" style="width: 60%; display: inline-block">
                                            <asp:CheckBox runat="server" ID="chkAllLanguages" />
                                            <span class="notes"><span style="color: black">Get All Language Versions</span> (overrides Language dropdown)</span>
                                        </div>

                                        <div class="row">
                                            <span class="header">Export Reference Field Values as Name Instead of Path</span>
                                            <asp:CheckBox runat="server" ID="chkDroplistName" />
                                            <span class="notes">By default, reference fields (droplist, multilist, etc) will export the full path of each selected item. Check this box to export name only</span>
                                        </div>

                                        <div class="row">
                                            <span class="header">Download File Name</span>
                                            <input runat="server" id="txtFileName" />
                                        </div>
                                    </div>
                                </div>



                            </div>
                        </div>
                        <br />
                        <br />


                        <div class="advanced open open-default" runat="server" id="divExportData">
                            <a class="advanced-btn">Fields and Data</a>
                            <div class="advanced-inner">
                                <div class="inner-section">
                                    <h3>Fields and Data</h3>
                                    <div class="row">
                                        <div class="inner-section">
                                            <span class="header">Fields</span><span class="notes">Note: Select <a href="#divFilters">Template filters</a> first if you want to browse only the fields of the selected templates</span>
                                            <a class="clear-btn" data-id="inputFields">clear</a>
                                            <textarea runat="server" id="inputFields" cols="60" row="5"></textarea><asp:Button runat="server" ID="btnBrowseFields" OnClick="btnBrowseFields_OnClick" CssClass="browse-btn" Text="Browse" />
                                            <span class="border-notes">Enter field names or IDs separated by commas, or use Browse to select fields.
                                        
                                            </span>
                                            <div class="">
                                                <asp:CheckBox runat="server" ID="chkAllFields" />
                                                <span class="notes"><b style="color: black">All Fields</b> - This will export the values of <b>all fields</b> of every included item. <b>This may take a while.</b></span>
                                            </div>
                                            <div class="">
                                                <asp:CheckBox runat="server" ID="chkComponentFields" />
                                                <span class="notes"><b style="color: black">Include Component Fields</b> - This will export the values of fields that are on a page's component items as well as on the page item itself</span>
                                            </div>
                                        </div>
                                    </div>


                                    <div class="row">
                                        <span class="header">Template Name</span>
                                        <asp:CheckBox runat="server" ID="chkIncludeTemplate" />
                                        <span class="notes">Export the template name of each item</span>
                                    </div>

                                    <div class="row">
                                        <span class="header">Item Name</span>
                                        <asp:CheckBox runat="server" ID="chkIncludeName" />
                                        <span class="notes">Export the name of each item</span>
                                    </div>

                                    <div class="row">
                                        <span class="header">Item ID</span>
                                        <asp:CheckBox runat="server" ID="chkIncludeIds" />
                                        <span class="notes">Export the ID of each item</span>
                                    </div>

                                    <div class="row">
                                        <span class="header">Linked Item IDs </span>
                                        <asp:CheckBox runat="server" ID="chkIncludeLinkedIds" />
                                        <span class="notes">Export the IDs of linked items (paths exported by default) (images, links, droplists, multilists)</span>
                                    </div>
                                    <div class="row">
                                        <span class="header">Raw HTML </span>
                                        <asp:CheckBox runat="server" ID="chkIncludeRawHtml" /><span class="notes">Export the raw HTML of applicable fields (images and links)</span>
                                    </div>

                                    <div class="row">
                                        <span class="header">Referrers</span>
                                        <asp:CheckBox runat="server" ID="chkReferrers" />
                                        <span class="notes">Export the paths of all items that refer to each item</span>
                                    </div>
                                    <div class="row">
                                        <span class="header">Related Items</span>
                                        <asp:CheckBox runat="server" ID="chkRelateItems" class="chkRelatedItems" />
                                        <span class="notes">Export the paths of all items each item refers to</span>
                                    </div>

                                    <div class="row">
                                        <span class="header">Delimiter</span>
                                        <input name="radDelimiter" type="radio" runat="server" id="radSemicolon" /><span><b>Semicolon</b> (Ready-friendly)</span><br />
                                        <span style="margin-left: 22px; display: block;">{E71C307E-E643-4CB7-9EE1-36B71BA0D6BD}; {4A193968-B0FE-4E85-B058-5871296786AB};</span><br />
                                        <input name="radDelimiter" type="radio" runat="server" id="radPipe" /><span><b>Pipe</b> (Code-friendly)</span><br />
                                        <span style="margin-left: 22px; display: block;">{E71C307E-E643-4CB7-9EE1-36B71BA0D6BD}|{4A193968-B0FE-4E85-B058-5871296786AB}</span><br />
                                    </div>

                                    <div class="advanced open open-default" runat="server" id="divStandardFields">
                                        <a class="advanced-btn">Standard Fields</a>
                                        <div class="advanced-inner">
                                            <div class="inner-section">
                                                <div class="row">
                                                    <span class="header">All Standard Fields</span>
                                                    <asp:CheckBox runat="server" CssClass="workflowBox" ID="chkAllStandardFields" />
                                                    <span class="notes">Export all standard fields (this is a lot of fields!)</span>
                                                </div>

                                                <h3>Workflow</h3>

                                                <div class="row">
                                                    <span class="header">Workflow</span>
                                                    <asp:CheckBox runat="server" CssClass="workflowBox" ID="chkWorkflowName" />
                                                    <span class="notes">Export the name of the workflow applied to this item</span>
                                                </div>
                                                <div class="row">
                                                    <span class="header">Workflow State</span>
                                                    <asp:CheckBox runat="server" CssClass="workflowBox" ID="chkWorkflowState" />
                                                    <span class="notes">Export the current workflow state (Workflow options require the database to be set to master)</span>
                                                </div>

                                                <h3>Publishing</h3>
                                                <div class="row">
                                                    <span class="header">Publish</span>
                                                    <asp:CheckBox runat="server" ID="chkPublish" />
                                                    <span class="notes">Publish date</span>
                                                </div>

                                                <div class="row">
                                                    <span class="header">Unpublish</span>
                                                    <asp:CheckBox runat="server" ID="chkUnpublish" />
                                                    <span class="notes">Unpublish date</span>
                                                </div>

                                                <div class="row">
                                                    <span class="header">Never Publish?</span>
                                                    <asp:CheckBox runat="server" ID="chkNeverPublish" />
                                                    <span class="notes">True/False</span>
                                                </div>

                                                <h3>Statistics</h3>
                                                <div class="row">
                                                    <span class="header">Date Created</span>
                                                    <asp:CheckBox runat="server" ID="chkDateCreated" />
                                                    <span class="notes">Date item was created</span>
                                                </div>
                                                <div class="row">
                                                    <span class="header">Created By</span>
                                                    <asp:CheckBox runat="server" ID="chkCreatedBy" />
                                                    <span class="notes">Sitecore user who created the item</span>
                                                </div>
                                                <div class="row">
                                                    <span class="header">Date Modified</span>
                                                    <asp:CheckBox runat="server" ID="chkDateModified" />
                                                    <span class="notes">Date item was last published</span>
                                                </div>
                                                <div class="row">
                                                    <span class="header">Modified By</span>
                                                    <asp:CheckBox runat="server" ID="chkModifiedBy" />
                                                    <span class="notes">Sitecore user who last published the item</span>
                                                </div>

                                                <h3>Security</h3>
                                                <div class="row">
                                                    <span class="header">Owner</span>
                                                    <asp:CheckBox runat="server" ID="chkOwner" />
                                                    <span class="notes">Sitecore user who owns the item</span>
                                                </div>

                                            </div>
                                        </div>
                                    </div>


                                </div>
                            </div>

                        </div>
                        <br />

                        <asp:Button class="spinner-btn" runat="server" ID="btnExport2" OnClick="btnRunExport_OnClick" Text="Run Content Export" /><br />
                        <br />
                        <br />
                        <br />

                        <div class="advanced open open-default" id="divAudits">
                            <a class="advanced-btn">Special Audits & Search</a>
                            <div class="advanced-inner">
                                <div class="row advanced-search">
                                    <span class="header"><b>Component Audit</b></span>
                                    <span class="notes">Run this export to audit the components on each Sitecore item. You can use the Start Item, Template and Created/Published Date filters and Language options to select items. The exported data will include the name of the component, the page it is on, and any associated datasource item
                                    </span>
                                    <br />
                                    <br />
                                    <asp:Button class="spinner-btn" runat="server" ID="btnComponentAudit" OnClick="btnComponentAudit_OnClick" Text="Run Component Audit" />

                                    <br />
                                    <br />
                                    <br />
                                    <span class="header"><b>Obsolete Component Audit</b></span>
                                    <span class="notes">Run this export to get all of the components that are not in use</span>
                                    <br />
                                    <br />
                                    <asp:Button class="spinner-btn" runat="server" ID="btnObsoleteComponentAudit" OnClick="btnObsoleteComponentAudit_Click" Text="Run Obsolete Component Audit" />
                                </div>
                                <div class="row advanced-search">
                                    <span class="header"><b>Rendering Parameters</b></span>
                                    <span class="notes">Run this export to get all of the Rendering Parameters on each Sitecore item. You can use the Start Item, Template and Created/Published Date filters and Language options to select items. This export will look the same as a Content Export, but will include Rendering Parameters rather than Template Fields.
                                        <br />
                                        Supported options: Template Name, Item Name, Item ID, Start Item(s), all Filters
                                    </span>
                                    <br />
                                    <br />
                                    <asp:Button class="spinner-btn" runat="server" ID="btnRenderingParametersAudit" OnClick="btnRenderingParametersAudit_Click" Text="Run Rendering Parameters Audit" />
                                </div>
                                <div class="row advanced-search">
                                    <span class="header"><b>Template Audit</b></span>
                                    <span class="notes">Run this export to audit the templates. This will generate a report of each Sitecore template and every instance where it is used.
                                    </span>
                                    <br />
                                    <br />
                                    <asp:CheckBox runat="server" ID="chkObsoleteTemplates" /><span class="notes"><b style="color: black">Obsolete templates</b> - Generate a report of all templates that are not in use</span><br />
                                    <br />
                                    <asp:Button class="spinner-btn" runat="server" ID="btnTemplateAudit" OnClick="btnTemplateAudit_OnClick" Text="Run Obsolete Template Audit Audit" />
                                </div>
                                <div class="row advanced-search">
                                    <span class="header"><b>Advanced Search:</b></span>
                                    <input runat="server" id="txtAdvancedSearch" /><asp:Button class="spinner-btn" runat="server" ID="btnAdvancedSearch" OnClick="btnAdvancedSearch_OnClick" Text="Go" />
                                    <span class="border-notes">Export all items that contain the search text in a field. 
                                    <br />
                                        By default, this will check ALL fields on each item; if fields are specified in the Fields box, only those fields will be searched
                                    <br />
                                        Advanced search works with the Start Item, Templates, and Fields boxes
                                    </span>
                                </div>
                            </div>
                            <br />
                            <br />
                        </div>

                        <div class="advanced open open-default" id="packageExport">
                            <a class="advanced-btn">Package Export</a>
                            <div class="advanced-inner">
                                <div class="row advanced-search">
                                    <h3>Package Export</h3>
                                    <p>Export a <b>Sitecore Package</b> instead of a CSV file</p>
                                    <p>
                                        This will generate a Sitecore Package of all of the items that would be included in the Export.<br />
                                        <br />
                                        The Package Export uses the same selection and filtering logic as the Content export. Click Get Package Preview to get a CSV report of all the items that will be included in the package.<br />
                                        <br />
                                        (NOTE: in the Content export, related items will be included in a column for each item; in the Package Preview, all items and subitems will be on their own line)
                                    </p>

                                    <p><b>WARNING: </b>Installing the package with the "overwrite" option will delete any subitems that are not included in the package. Review the Summary carefully especially when using the Related Items option</p>

                                    <div class="row">
                                        <asp:CheckBox runat="server" ID="chkIncludeRelatedItems" class="chkRelatedItems" /><span class="notes"><b style="color: black">Include Related Items</b></span><br />
                                        <span style="color: black" class="notes">Include all related items of each exported item in the package.
                                        <br />
                                            This will include all related items <b>and all related items of related items</b>.<br />
                                            The Content Export will only show the directly related items, but the preview will show all related items that will be included in the package.
                                        </span>
                                    </div>

                                    <div class="row">
                                        <asp:CheckBox runat="server" ID="chkIncludeSubitems" /><span class="notes"><b style="color: black">Include Subitems</b></span><br />
                                        <span style="color: black" class="notes">Include all subitems of each exported item in the package.
                                        <br />
                                            This will include <b>all subitems of every exported item</b>, ignoring filters such as Template type
                                        <br />
                                            This will negate the <b>No children</b> checkbox
                                        
                                        <br />
                                            <br />
                                            <span class="notes">Example: Used the template and date range filters to select specific items, then check off Include Subitems to include the subitems of all the items in the filtered selection</span>
                                        </span>
                                    </div>

                                    <asp:Button runat="server" class="spinner-btn" ID="btnPackageExport" Text="Begin Package Export" OnClick="btnPackageExport_OnClick" />
                                    <br />
                                    <br />
                                    <asp:Button runat="server" class="spinner-btn" ID="btnPackageSummary" Text="Get Package Preview" OnClick="btnPackageSummary_OnClick" /><br />
                                    <span class="notes">Download a CSV file that shows all of the items included in the package</span>
                                </div>
                            </div>
                        </div>
                        <br />
                        <br />
                        <div class="advanced open open-default" id="contentImport">
                            <a class="advanced-btn">Content Import</a>
                            <div class="advanced-inner">
                                <div class="row advanced-search">
                                    <span style="color: red" class="uploadResponse">
                                        <asp:Literal runat="server" ID="litUploadResponse"></asp:Literal></span>
                                    <%--                                <span class="header"><b>Advanced Search:</b></span>--%>
                                    <asp:FileUpload runat="server" ID="btnFileUpload" Text="Upload File" />
                                    <span class="" style="display: block; margin-top: 10px;">
                                        <b>Getting Started</b><br />
                                        To create new items, CSV must include the following columns: <b>Item Path</b>, <b>Template</b>, <b>Name</b>. In the Item Path field, put in the path of the parent item.
                                    <br />
                                        <br />
                                        To edit existing items, CSV must include <b>Item Path</b>
                                        <br />
                                        <br />
                                        By default, the import will NOT overwrite exising items, but will only create new items.
                                    <br />
                                        To overwrite existing items, uncheck the checkbox below
                                    <br />
                                        <br />

                                        <input name="radImpport" type="radio" runat="server" id="radImport" /><span><b>Create</b></span> new items using a specified Template (existing items will be ignored)<br />
                                        <input name="radImpport" type="radio" runat="server" id="radUpdate" /><span><b>Update</b></span> existing items based on the item <b>Id</b> or <b>path</b> (new items will not be created) 
                                        
                                        <hr />

                                        <input name="radImpport" type="radio" runat="server" id="radPublish" /><span><b>Publish</b></span> existing items based on the item <b>Id</b> or <b>path</b>. This is for items that <b>already exist</b> that you want to publish.
                                        <br />
                                        <input name="radImpport" type="radio" runat="server" id="radDelete" /><span><b>Delete</b></span> existing items based on the item <b>Id</b> or <b>path</b>. <b>Use with caution!</b> Check off the <b>publish</b> option below to publish deletions
                         
                                    <br />
                                        <br />
                                    </span>

                                    <div class="row">
                                        <asp:CheckBox runat="server" ID="chkNoDuplicates" /><span class="notes"><b style="color: black">Do not create duplicates</b></span><br />
                                        <span class="notes">If this box is checked off, Create Items will not create a new item if an item with the same name and template already exists in that location</span>
                                    </div>

                                    <div class="row">
                                        <asp:CheckBox runat="server" ID="chkPublishChanges" /><span class="notes"><b style="color: black">Publish changes</b></span><br />
                                        <span class="notes">Check this box to automatically publish all changes made during the import. This will <b>only</b> publish the items specified in the CSV (no children or parents)</span>
                                    </div>

                                    <div class="row">
                                        <span class="header"><b>Publishing Target</b></span>
                                        <asp:DropDownList runat="server" ID="ddPublishDatabase" CssClass="ddDatabase" />
                                        <span class="notes">Select database to publish to</span>
                                    </div>

                                    <asp:Button runat="server" ID="btnBeginImport" CssClass="spinner-btn" Text="Begin Import" OnClick="btnBeginImport_OnClick" />

                                    <br />
                                    <br />


                                    <a href="javascript:void(0)" class="btnSampleLink">Download Sample CSV</a>

                                    <div class="modal browse-modal" style="display: none" id="singleTemplateModal">
                                        <div style="width: 100%" class="select-box left" id="singleTemplate">
                                            <div class="content">
                                                <b style="padding: 0 20px;">Select a template to generate a sample CSV, or just click Download to get a basic CSV sample</b>
                                                <ul style="margin-top: 10px;">
                                                    <li data-name="templates" data-id="{3C1715FE-6A13-4FCF-845F-DE308BA9741D}"><a class="browse-expand" onclick="expandNode($(this))">+</a><span></span><span>templates</span></li>
                                                </ul>
                                            </div>
                                            <div class="buttons">
                                                <asp:TextBox runat="server" Style="display: none" ID="txtSampleTemplate"></asp:TextBox>
                                                <a class="btn start-import spinner-button" onclick="downloadSample()">Download</a>
                                                <a class="btn close-modal" onclick="closeTemplatesModal()">Cancel</a>
                                            </div>
                                        </div>
                                    </div>

                                    <asp:Button class="spinner-btn" Style="display: none;" runat="server" ID="btnDownloadCSVTemplate" Text="Download Sample" OnClick="btnDownloadCSVTemplate_OnClick" />

                                    <h3>READ ME!</h3>
                                    <p>
                                        Use the import tool carefully! Make sure to review all modified items in Sitecore before publishing.
                                    <br />
                                        <br />
                                        The <b>Update</b> option will only edit existing items (found using the Item Path) and will ignore items that are not found.
                                    <br />
                                        <br />
                                        The <b>Import</b> button will create new items under the Item Path. An item will not be created if an item with the same path and template already exists, unless you uncheck "Do not create duplicates"
                                    </p>

                                    <h3>Tips:</h3>
                                    <ul>
                                        <li>Files should be uploaded in <b>csv</b> format</li>
                                        <li>Item Path and Template can be either a full Sitecore path or a Guid ID</li>
                                        <li>Add a column for every field that you want to add/change content for with the field name or ID (e.g. replace Field1 in the example template with a valid field name)
                                        <ul>
                                            <li>If you are editing content, it is recommended to export all the items with all fields you want to modify first, edit that file and then upload it</li>

                                        </ul>
                                        </li>
                                        <li>If you are modifying existing content, for best results run an export on that content first, make  your changes in the downloaded file and re-upload that file to import.</li>
                                        <li>To <b>edit</b> content, Item Path must be the path of the item you with to edit.<br />

                                        </li>
                                        <li>To <b>create</b> content, the Item Path must be the path of the parent item you wish to create the new item under (parent item must already exist);
                                        <ul>
                                            <li>Make sure to include Name and Template when creating items
                                            </li>
                                            <li>Name and Template are not necessary for editing items
                                            </li>
                                        </ul>
                                        </li>
                                        <li>Note: The import function currently supports string, image, and link fields. It does not support more complex field types, such as droplists or multilists.</li>

                                        <li><b>Language Versions</b>: To specify language, add a <b>Language</b> column. If no language is specified, all items will be created/edited in the default language
                                        <ul>
                                            <li><b>Accepted language values:</b> </li>
                                            <li>
                                                <asp:Literal ID="litLanguageList" runat="server"></asp:Literal></li>
                                        </ul>
                                        </li>
                                        <li>If you want to <b>create</b> new language versions of <b>existing items</b>, use the <b>EDIT</b> option</li>
                                    </ul>

                                </div>

                            </div>
                        </div>
                        <br />
                        <br />
                        <div class="advanced open open-default" id="renderingParamsImport">
                            <a class="advanced-btn">Rendering Parameters Import</a>
                            <div class="advanced-inner">
                                <div class="row advanced-search">
                                    <span style="color: red" class="uploadResponse">
                                        <asp:Literal runat="server" ID="litUploadRenderingParamResponse"></asp:Literal></span>
                                    <asp:FileUpload runat="server" ID="btnRenderingParamFileUpload" Text="Upload File" />
                                    <span class="" style="display: block; margin-top: 10px;">
                                        <b>Getting Started</b><br />

                                        Use this import method to modify the rendering parameters (FINAL LAYOUT) of the components on your Sitecore items.
                                        <br />
                                        <br />
                                        This import is recommended for when ou have a rendering(s) that exists on a large number of pages, and need to:
                                        <ul>
                                            <li>
                                                change the placeholder that it lives in on every page.
                                            </li>
                                            <li>
                                                change its position on the page (i.e. make it first, or put it above or below another rendering) on every page.
                                            </li>
                                            <li>
                                                change the value of one of the rendering parameters on every page.
                                            </li>
                                        </ul>
                                        <b style="color:red">Caution!</b><br /> Renderings on a page do not have an ID and can only be identified by name. You can use the <b>When Placeholder Equals</b> and <b>Nth of Type</b> columns to specify which rendering to modify.
                                        If there are multiple renderings of the same name, by default only the <b>first</b> matching rendering will be modified. <b>When Placeholder Equals</b> and <b>Nth of Type</b> can be used in combination to get the nth rendering of that name within a particular placeholder.

                                    </span>
                                    <br />

                                    <ul>
                                        <li><b>Item Path</b> <span class="notes">This column should contain the item path or ID</span></li>
                                        <li><b>Apply to All Subitems</b> <span class="notes">Apply the changes on this line to the item and all subitems, defaults to false (TRUE/FALSE)</span></li>
                                        <li><b>Template</b> <span class="notes">With Apply to All Subitems, apply the changes only to items with the specified template name or ID</span></li>
                                        <li><b>Component Name</b> <span class="notes">The name or ID of the component to modify</span></li>
                                        <li><b>When Placeholder Equals</b> <span class="notes">Modify a component within this particular placeholder</span></li>
                                        <li><b>Nth of Type</b> <span class="notes">Modify the Nth component with the specified name (NUMERIC, STARTS AT 1)</span>
                                            <ul>
                                                <li><span class="notes">With <b>When Placeholder Equals</b>, modify the Nth component within the specified placeholder with the specified name</span></li>
                                            </ul>
                                        </li>
                                        <li><b>Parameter Name</b> <span class="notes">The name of the rendering parameter to modify or add</span></li>
                                        <li><b>Value</b> <span class="notes">The value to set for the rendering parameter</span></li>
                                        <li><b>Placeholder</b> <span class="notes">The placeholder to move the rendering to</span></li>
                                        <li><b>Position</b> <span class="notes">The position to put the rendering in relative to all other renderings (NUMERIC, STARTS AT 0)</span></li>
                                        <li><b>Position in Placeholder</b> <span class="notes">The position to put the rendering in relative to its placeholder (NUMERIC, STARTS AT 0)</span></li>
                                        <li><b>Before</b> <span class="notes">The name of the FIRST rendering to put this rendering before</span></li>
                                        <li><b>After</b> <span class="notes">The name of the LAST rendering to put this rendering after</span></li>
                                    </ul>

                                    <div class="row">
                                        <asp:CheckBox runat="server" ID="chkPublishRenderingParamChanges" /><span class="notes"><b style="color: black">Publish changes</b></span><br />
                                        <span class="notes">Check this box to automatically publish all changes made during the import. This will <b>only</b> publish the items specified in the CSV (no children or parents)</span>
                                    </div>

                                    <div class="row">
                                        <span class="header"><b>Publishing Target</b></span>
                                        <asp:DropDownList runat="server" ID="ddRenderingParamPublishDatabase" CssClass="ddDatabase" />
                                        <span class="notes">Select database to publish to</span>
                                    </div>

                                    <asp:Button runat="server" ID="btnBeginRenderingParamImport" CssClass="spinner-btn" Text="Begin Import" OnClick="btnBeginRenderingParamImport_Click" />

                                    <br />
                                    <br />

                                    <asp:Button class="spinner-btn" runat="server" ID="btnDownloadRenderingParamsSample" Text="Download Template" OnClick="btnDownloadRenderingParamsSample_Click" />

                                    <h3>READ ME!</h3>
                                    <ul>
                                        <li>Blank cells will be skipped on each line; if a column is blank for a particular item, it will be ignored</li>
                                        <li>You will need a <b>new line</b> for each rendering parameter you want to change; you may have multiple lines with the same Item Path</li>
                                        <li>The columns will be executed in the order listed above
                                            <ul>
                                                <li>If Placeholder and Position in Placeholder are both populated, the placeholder will be changed first and then the position within that placeholder will be updated</li>
                                                <li>If Position and Position in Placeholder are both populated, Position in Placeholder will override Position</li>
                                                <li>If Position in Placeholder and Before/After are both populated, Before/After will be executed last which will override the Position in Placeholder</li>
                                                <li>If Before and After are both populated, After will override Before</li>
                                            </ul>
                                        </li>
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
