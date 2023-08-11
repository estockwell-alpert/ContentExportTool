# ContentExportTool
Custom tool to export, modify, and create Sitecore content

# Installation:
Use the Installation Wizard to install Content Export Tool.zip from the latest release

<b>You can get the installation package from the Installation Packages folder, or from the Releases</b> 
- If you are on Azure, choose one of the Content Export Tool for Azure packages. Choose 9.0 if you are on 9.0-9.1 or 9.2 if you are on 9.2+. 
- If you are not on Azure, install Content Export Tool v9.1 (latest stable non-Azure build) 
- If you want the Media Export, use the v8.9 - Media Export release. This release is NOT up to date with the latest features, as the Media Export has known issues and is not supported at this time. However the package is still available for those who want to use it
- If the Media Export version does not work, install the latest version that does not have the media export
- The Azure versions CAN be installed on non-Azure instances, but is not recommended


# Troubleshooting:
If the Content Export Tool shows a compilation error when you open it, delete the method from the aspx.cs file (and the corresponding buttons from the aspx file) that is causing the problem. For example, older Sitecore versions may run into a compilation error with the Rendering Parameters Import feature. Files can be found in your Sitecore website under /sitecore/shell/Applications/ContentExport and edited in Notepad++, no build required

# Dependencies:
LATEST VERSION: Sitecore 8+

For older versions of Sitecore, download https://github.com/estockwell-alpert/ContentExportTool/releases/download/8.3/Content.Export.Tool.for.Sitecore.6.zip 

Releases: https://github.com/estockwell-alpert/ContentExportTool/releases

# To use:
You must be logged into Sitecore to access the tool<br />
Access the tool in the Sitecore start menu or at [your site]/sitecore/shell/applications/contentexport/ContentExport.aspx

# Documentation:
https://ericastockwellalpert.wordpress.com/2017/08/24/content-export-tool-for-sitecore/
https://ericastockwellalpert.wordpress.com/2018/11/30/sitecore-content-importing-use-a-csv-file-to-create-or-edit-sitecore-content/

# Video Tutorials:
https://www.youtube.com/watch?v=0BsvOTfuuWs - Basic Content Export tutorial<br/>
https://www.youtube.com/watch?v=L5ynC-Ev5zk - Import and Package Export tutorial

# Security:
Sitecore users can only view/export items that they have read permissions on. In the import feature, users can only create or modify items if they have write permissions.

# Files Included in Package:
 core:/sitecore/content/Applications/Content Export <br/>
 core:/sitecore/content/Documents and settings/All users/Start menu/Right/Content Export <br/>
 master:/sitecore/system/Modules/Content Export Tool <br/>
 master:/sitecore/templates/Modules/Content Export Tool/ <br/>
 /sitecore/shell/applications/contentexport/ContentExport.aspx	<br/>
 /sitecore/shell/applications/contentexport/ContentExport.aspx.cs	<br/>
 /sitecore/shell/applications/contentexport/ContentExport.aspx.designer.cs <br/>	
 /sitecore/shell/applications/contentexport/ContentExportScripts.js <br/>
 /temp/IconCache/Network/16x16/download.png	<br/>
 /temp/IconCache/Network/24x24/download.png	<br/>
 /temp/IconCache/Network/32x32/download.png	<br/>
 /sitecore/shell/Themes/Standard/Images/ProgressIndicator/sc-spinner32.gif

# Testimonials:
I used the Content Export Tool to update field values quickly on over 80 Sitecore forms and their sub-items. Multiple different value changes needed to be made to hundreds of fields over more than 80 forms. The Content Export Tool was exceptionally useful in this scenario because a specific set of fields and their values, tied to the value of another field on the item, needed to be updated where updating Standard Values would've updated all of them. We were looking to apply CSS classes to this specific subset of Forms items. I was able to use the filters available on through the tool to target just the items I needed to change, export just the field values I needed to change, update the values quickly in the CSV, and then upload the updated CSV to update the field values for all those items. Doing the change by hand would've required at least several hours and greatly increased the risk of a mistake. I subsequently used the tool to make structure changes to the forms. We had a a specific Form field type that needed to be wrapped in a Forms Section. I used the tool to create the new sections (create), create duplicate items under the new section (move), and remove the old items (delete). 

- Matt Richardson, Velir
