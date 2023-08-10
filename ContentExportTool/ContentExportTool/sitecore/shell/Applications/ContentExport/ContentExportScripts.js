$(document).ready(function () {

    $("#txtStartDateCr, #txtStartDatePb, #txtEndDateCr, #txtEndDatePu, .datepicker").datepicker();

    $(".btnSampleLink").on("click", function () {
        $("#singleTemplateModal").show();
    });

    $(".chkRelatedItems input[type='checkbox']").on("click", function () {
        $(".chkRelatedItems input[type='checkbox']").prop("checked", $(this).prop("checked"));
    });

    var loadingModalHtml = "<div class='loading-modal'><div class='loading-box'><div class='loader'></div></div></div>";

    $(".browse-btn", ".save-btn-decoy").on("click", function () {
        $(".feedback").empty();
        $(".loading-modal").show();
    });

    $("#btnComponentAudit").on("click", function () {
        showModal();
    })

    $(".spinner-btn").on("click", function () {
        showModal();
    });

    var showModal = function () {
        $(".feedback").empty();
        $(".loading-modal").show();
        //$("#loading-text").html(loadingModalHtml);
        var downloadToken = new Date().getTime();
        $("#txtDownloadToken").val(downloadToken)
        checkIfFileDownloaded(downloadToken);
    }

    $(".advanced-btn").on("click", function () {
        if ($(this).parent().hasClass("open")) {
            $(this).parent().removeClass("open");
        } else {
            $(this).parent().addClass("open");
        }

        $(this).parent().find(".advanced-inner").slideToggle();
    });

    $(".ddDatabase").on("change", function () {
        if ($(this).find("option:selected").val() === "custom") {
            $(".txtCustomDatabase").show();
        } else {
            $(".txtCustomDatabase").hide();
        }

        if ($(this).find("option:selected").val() !== "master") {
            $(".workflowBox input").each(function () {
                $(this).prop("checked", false);
            });
        }
    });

    $(".workflowBox input").on("change", function () {
        if ($(this).is(":checked")) {
            $(".ddDatabase").val("master");
        }
    });

    $(".clear-btn").on("click", function () {
        var id = $(this).attr("data-id");
        var input = $("#" + id);
        $(input).val("");
        removeSavedMessage();
    });

    $(".clear-section-btn").on("click", function () {
        $(this).parent().find("input").val("");
        removeSavedMessage();
    })

    $("#clear-fast-query").on("click", function () {
        $(".lit-fast-query").html("");
    });

    $(".show-hints").on("click", function () {
        $(this).next(".notes").slideToggle();
    });

    $(".save-btn-decoy").on("click", function () {
        var saveName = $("#txtSaveSettingsName").val();
        if (saveName === "") {
            $(".error-message").show();
            $(".save-settings-box input[type='text']").css("border", "1px solid red");
        } else {
            $("#btnSaveSettings").click();
        }
    });

    $("input").on("change", function () {
        removeSavedMessage();
    });

    $("select").on("change", function () {
        removeSavedMessage();
    });

    $("#chkAdvancedSelectionOn").on("change", function () {
        if ($(this).prop("checked")) {
            $(this).parent().addClass("disabled");
        } else {
            $(this).parent().removeClass("disabled");
        }
    });
});

function openAdvancedOptions() {
    if (!$("#divAdvOptions").hasClass("open")) {
        var btn = $("#divAdvOptions .advanced-btn");
        $(btn).click();
    }
}

function getFields(node) {
    if (!($(node).parent().hasClass("loaded"))) {
        // load children
        var itemId = $(node).parent().attr("data-id");

        loadFields(itemId, $(node).parent());
    }

    expandOrClose(node);
}

function expandNode(node) {

    if (!($(node).parent().hasClass("loaded"))) {
        // load children
        var itemId = $(node).parent().attr("data-id");

        loadChildren(itemId, $(node).parent());
    }

    expandOrClose(node);
}

function expandOrClose(node) {
    if ($(node).parent().hasClass("expanded")) {

        var children = $(node).parent().find("li");
        $(children).removeClass("expanded");
        var childBtns = $(node).parent().find(".browse-expand");
        $(childBtns).html("+");

        $(node).parent().removeClass("expanded");
        $(node).html("+");
    } else {
        $(node).parent().addClass("expanded");
        $(node).html("-");
    }
}

function getItemChildren(pathOrId) {
    return $.ajax({
        method: "get",
        url: "/sitecore/shell/applications/contentexport/contentexport.aspx",
        data: { getitems: true, startitem: pathOrId, database: $("#ddDatabase").val() }
    });
}

function getFieldsAsync(pathOrId) {
    return $.ajax({
        method: "get",
        url: "/sitecore/shell/applications/contentexport/contentexport.aspx",
        data: { getfields: true, startitem: pathOrId }
    });
}

function loadFields(id, parentNode) {
    var ul = $(parentNode).find(".field-list");
    $(ul).append("<img class='scSpinner' width='10' src='/sitecore/shell/themes/standard/Images/ProgressIndicator/sc-spinner32.gif'/>");
    var innerHtml = "";

    getFieldsAsync(id).then(function (results) {
        if (results.length) {
            var children = results;

            for (var i = 0; i < children.length; i++) {
                var child = children[i];

                var hasChildren = child.HasChildren;
                var id = child.Id;
                var name = child.Name;
                var path = child.Path;

                var selected = false;
                var selectedMatch = $(".selected-box a.addedTemplate[data-path='" + name + "']");
                if (selectedMatch.length > 0) {
                    selected = true;
                }

                var fieldNode = "<li data-name='" + name + "'><a class='field-node " + (selected ? "disabled" : "") + "' href='javascript:void(0)' onclick='selectBrowseNode($(this));' ondblclick='selectBrowseNode($(this));addTemplate();' data-id='" + id + "' data-path='" + name + "'>" + name + "</a></li>";

                innerHtml += fieldNode;
            }
            $(ul).find(".scSpinner").remove();
            $(ul).append(innerHtml);

            $(parentNode).addClass("loaded");
        }
    });
}


function loadChildren(id, parentNode) {
    $(parentNode).append("<img class='scSpinner' width='10' src='/sitecore/shell/themes/standard/Images/ProgressIndicator/sc-spinner32.gif'/>");
    var innerHtml = "<ul>";
    var templates = isTemplate() || $(parentNode).parents("#singleTemplateModal").length === 1;
    getItemChildren(id).then(function (results) {
        if (results.length) {
            var children = results;

            for (var i = 0; i < children.length; i++) {
                var child = children[i];

                var hasChildren = child.HasChildren;
                var id = child.Id;
                var name = child.Name;
                var path = child.Path;

                var childNode = "<li data-name='" + name + "' data-id='" + id + "' data-path='" + path + "'>";

                if (hasChildren) {
                    childNode += "<a class='browse-expand' onclick='expandNode($(this))'>+</a>";
                }

                if (templates) {
                    var itemTemplate = child.Template.split('/')[child.Template.split('/').length - 1];
                    if (itemTemplate === "Template") {
                        childNode += getClickableBrowseItem(path, name);
                    } else {
                        childNode += "<span class='sitecore-node'>" + name + "</span>";
                    }
                } else {
                    childNode += getClickableBrowseItem(path, name);
                }

                childNode += "</li>";
                innerHtml += childNode;
            }
            innerHtml += "</ul>";
            $(parentNode).find(".scSpinner").remove();
            $(parentNode).append(innerHtml);

            $(parentNode).addClass("loaded");
        }
    });
}

function checkIfFileDownloaded(downloadToken) {
    var token = getCookie("DownloadToken");

    if ((token == downloadToken)) {
        //$("#loading-text").html("");
        $(".loading-modal").hide();
        expireCookie("DownloadToken");
    } else {
        setTimeout(function () {
            checkIfFileDownloaded(downloadToken)
        }, 1000)
    }
}

function getCookie(name) {
    var parts = document.cookie.split(name + "=");
    if (parts.length == 2) return parts.pop().split(";").shift();
}

function expireCookie(cName) {
    document.cookie =
        encodeURIComponent(cName) + "=deleted; expires=" + new Date(0).toUTCString();
}

function getClickableBrowseItem(path, name) {
    var selected = false;
    var selectedMatch = $(".selected-box a.addedTemplate[data-path='" + path + "']");
    if (selectedMatch.length > 0) {
        selected = true;
    }
    return "<a class='sitecore-node " + (selected ? "disabled" : "") + "' href='javascript:void(0)' ondblclick='selectNode($(this));addTemplate();' onclick='selectNode($(this));' data-path='" + path + "' data-name='" + name + "'>" + name + "</a>";
}

function isTemplate() {
    if ($("#divBrowseContainer").hasClass("templates")) {
        return true;
    }
    return false;
}

function isExcludeTemplates() {
    if ($("#divBrowseContainer").hasClass("exclude-templates")) {
        return true;
    }
    return false;
}

function isField() {
    return ($(".browse-modal").hasClass("fields"));
}

function selectNode(node) {

    // if link is in the template model:
    selectBrowseNode(node);
}

function removeSavedMessage() {
    $(".save-message").html("");
}

function selectBrowseNode(node) {
    $(".browse-modal a").removeClass("selected");
    $(node).addClass("selected");
    $(".temp-selected").html($(node).attr("data-path"));
}

function addTemplate() {
    var path = $(".temp-selected").html();
    var node = $(".select-box a[data-path='" + path + "']");
    $(node).addClass("disabled").removeClass("selected");
    $(".selected-box-list").append("<li><a class='addedTemplate' href='javascript:void(0);' onclick='selectAddedTemplate($(this))' ondblclick='selectAddedTemplate($(this));removeTemplate()' data-name='" + name + "' data-path='" + path + "'>" + path + "</a></li>");
    $(".temp-selected").html("");
}

function selectAddedTemplate(node) {
    $(".browse-modal a").removeClass("selected");
    $(node).addClass("selected");
    $(".temp-selected-remove").html($(node).html());
}

function downloadSample() {
    var templateNode = $("#singleTemplate").find(".selected");
    $("#txtSampleTemplate").val(templateNode.attr("data-path"));
    $("#singleTemplate .close-modal").click();

    var downloadToken = new Date().getTime();
    $("#txtDownloadToken").val(downloadToken);

    $("#btnDownloadCSVTemplate").click();
    $(".loading-modal").show();

    checkIfFileDownloaded(downloadToken);
}

function removeTemplate() {
    var path = $(".temp-selected-remove").html();
    var node = $(".selected-box a.addedTemplate[data-path='" + path + "']");
    $(node).parent().remove();
    var origNode = $(".select-box a[data-path='" + path + "']");
    origNode.removeClass("disabled");
}

function confirmBrowseSelection() {
    var str = getSelectedString();
    if (isExcludeTemplates()) {
        $("#inputExcludeTemplates").html(str);
    }
    else if (isTemplate()) {
        $("#inputTemplates").html(str);
    } else {
        $("#inputStartitem").html(str);
    }

    closeTemplatesModal();
}

function closeTemplatesModal() {
    //var tree;
    //if (isTemplate()) {
    //    tree = $(".browse-modal.templates .select-box.left").html().trim();
    //    $("#txtStoreTemplatesTree").val(tree);
    //} else {
    //    tree = $(".browse-modal.content .select-box.left").html().trim();
    //    $("#txtStoreContentTree").val(tree);
    //}   

    $(".browse-modal").hide();
}

function closeFieldModal() {
    //var tree = $(".browse-modal.fields .select-box.left").html().trim();
    //$("#txtStoreFieldsTree").val(tree);

    $(".browse-modal.fields").hide();
}

function confirmFieldSelection() {
    var fieldString = getSelectedFields();
    $("#inputFields").html(fieldString);
    closeFieldModal();
}

function getSelectedFields() {
    var selectedString = "";
    var selectedItems = $(".selected-box ul li");
    for (var i = 0; i < selectedItems.length; i++) {
        if (i > 0) {
            selectedString += ", ";
        }
        selectedString += $(selectedItems[i]).find("a").html();
    }
    return selectedString;
}

function getSelectedString() {
    var selectedString = "";
    var selectedItems = $(".selected-box ul li");
    for (var i = 0; i < selectedItems.length; i++) {
        if (i > 0) {
            selectedString += ", ";
        }
        selectedString += $(selectedItems[i]).find("a").attr("data-path")
    }
    return selectedString;
}

function selectAllFields(node) {
    var fields = $(node).next().find("li");
    for (var i = 0; i < fields.length; i++) {
        var fieldNode = $($(fields)[i]).find("a");
        $(".temp-selected").html($(fieldNode).html());
        addTemplate();
    }
}

function browseSearch(searchbar) {
    var term = $(searchbar).val();
    $(".browse-modal .left li").addClass("hidden");
    var list = $(".browse-modal li[data-name*='" + term + "']");
    for (var i = 0; i < list.length; i++) {
        var li = list[i];
        $(li).removeClass("hidden");
        var parents = $(li).parents("li");
        for (var j = 0; j < parents.length; j++) {
            $(parents[j]).removeClass("hidden");
            if (!$(parents[j]).hasClass("expanded")) {
                $(parents[j]).find("a.browse-expand").click();
            }
        }
    }
}

function clearModalSelections() {
    $(".selected-box-list").empty();
    $(".browse-modal .left a.disabled").removeClass("disabled");
}

function clearSearch(btn) {
    var searchbar = $(btn).parent().find("input.field-search");
    $(searchbar).val("");
    var expanded = $("li.expanded");
    for (var i = 0; i < expanded.length; i++) {
        $(expanded[i]).find("a.browse-expand").click();
    }
    $(".browse-modal li").removeClass("hidden");
}

function confirmDelete() {
    var settings = $("#ddSavedSettings").val();
    var confirmation = confirm("Are you sure you want to delete '" + settings + "'?");
    if (confirmation) {
        $(".btn-delete").click();
    }
}

