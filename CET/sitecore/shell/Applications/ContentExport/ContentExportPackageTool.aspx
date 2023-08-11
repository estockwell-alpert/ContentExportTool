<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ContentExportPackageTool.aspx.cs" Inherits="ContentExportTool.ContentExportAdminPage" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
</head>

<body>
    <form id="form1" runat="server">
        Name: <input runat="server" id="txtFileName" /><br/>
        Version: <input runat="server" id="txtVersion"/><br/>
        <asp:Button runat="server" ID="btnGeneratePackage" OnClick="btnGeneratePackage_OnClick" Text="Create Package" />
    </form>
</body>
