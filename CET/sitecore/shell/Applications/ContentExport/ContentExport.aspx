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

        .ui-datepicker {
            background: white;
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
    </head>
    <body>

        <form id="form1" runat="server">
            <div class="container feedback">
                <asp:Literal runat="server" ID="litFeedback"></asp:Literal>
            </div>
            <div class="container">
                <div class="row">
                                <span class="header" id="startitems">Start Item(s)</span>
                                <a class="clear-btn" data-id="inputStartitem">clear</a>
                                <textarea runat="server" id="inputStartitem" />
                                <span class="border-notes">Enter the ID of each starting node separated by commas (MUST BE GUIDs).<br />
                                    Only content beneath and including this node will be exported. If field is left blank, the starting node will be /sitecore/content.</span>
                </div>

                <div class="row">
                    <span class="header"><b>Templates</b></span>
                    <a class="clear-btn" data-id="inputTemplates">clear</a>
                    <textarea runat="server" id="inputTemplates" cols="60" row="5"></textarea>
                    <span class="border-notes">Enter template IDs separated by commas (MUST BE GUIDs)
                    <br />
                        Items will only be exported if their template is in this list. If this field is left blank, all templates will be included.</span>
                    <div class="hints">
                        <a class="show-hints">Hints</a>
                        <span class="notes">Example: Standard Page, {12345678-901-2345-6789-012345678901}
                        </span>
                    </div>
                </div>

                <div class="row">
                    <span class="header">Fields</span>
                    <a class="clear-btn" data-id="inputFields">clear</a>
                    <textarea runat="server" id="inputFields" cols="60" row="5"></textarea>
                    <span class="border-notes">Enter field names separated by commas.
                                        
                    </span>

                    <div class="">
                        <asp:CheckBox runat="server" ID="chkAllFields" />
                        <span class="notes"><b style="color: black">All Fields</b> - This will export the values of <b>all fields</b> of every included item. <b>Note that this requires the Template names to be entered below</b></span> <br />
                        <textarea runat="server" id="txtFieldTemplates" cols="60" row="5"></textarea>
                        <span class="border-notes">Enter template NAMES separated by commas. This will not filter; it will only retrieve fields
                    </div>
                </div>

                <asp:Button class="spinner-btn content-export-btn" runat="server" ID="btnExport2" OnClick="btnRunExport_OnClick" Text="Run Content Export" /><br />
            </div>
        </form>
    </body>
</html>