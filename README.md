# ContentExportTool
Custom tool to audit Sitecore content

# Installation:
Use the Installation Wizard to install Content Export Tool.zip

# Dependencies:
Sitecore 6.5 or higher<br />

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
