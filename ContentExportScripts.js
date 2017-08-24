$(document).ready(function () {

    $(".advanced-btn").on("click", function () {
        if ($(this).parent().hasClass("open")) {
            $(this).parent().removeClass("open");
        } else {
            $(this).parent().addClass("open");
        }

        $(".advanced-inner").slideToggle();
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
});

function expandNode(node) {
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

function selectNode(node) {
    $(".select-node-btn").removeClass("disabled");
    var nodePath = $(node).attr("data-path");
    $(".selected-node").html(nodePath);
}

function confirmSelection() {
    var nodePath = $(".selected-node").html();
    closeTreeBox();
    $("#inputStartitem").val(nodePath);
}

function closeTreeBox() {
    $(".browse-modal").hide();
}

function removeSavedMessage() {
    $(".save-message").html("");
}

function selectBrowseNode(node) {
    $(".browse-modal a").removeClass("selected");
    $(node).addClass("selected");
    $(".temp-selected").html($(node).html());
}

function addTemplate() {
    var name = $(".temp-selected").html();
    var node = $(".select-box a[data-name='" + name + "']");
    $(node).addClass("disabled").removeClass("selected");
    $(".selected-box-list").append("<li><a class='addedTemplate' href='javascript:void(0);' onclick='selectAddedTemplate($(this))' data-name='" + name + "' >" + name + "</a></li>");
    $(".temp-selected").html("");

    $(".selected-box .select-node-btn").removeClass("disabled");
}

function selectAddedTemplate(node) {
    $(".browse-modal.templates a").removeClass("selected");
    $(node).addClass("selected");
    $(".temp-selected-remove").html($(node).html());
}

function removeTemplate() {
    var name = $(".temp-selected-remove").html();
    var node = $(".selected-box a.addedTemplate[data-name='" + name + "']");
    $(node).parent().remove();
    var origNode = $(".select-box a[data-name='" + name + "']");
    origNode.removeClass("disabled");

    enableDisableSelect();
}

function enableDisableSelect() {
    var selectedTemplates = $(".selected-box ul li");
    if (selectedTemplates.length < 1) {
        $(".selected-box .select-node-btn").addClass("disabled");
    }
}

function confirmTemplateSelection() {
    var templateString = getSelectedString();
    $("#inputTemplates").html(templateString);
    closeTemplatesModal();
}

function closeTemplatesModal() {
    $(".browse-modal.templates").hide();
}

function closeFieldModal() {
    $(".browse-modal.fields").hide();
}

function confirmFieldSelection() {
    var fieldString = getSelectedString();
    $("#inputFields").html(fieldString);
    closeFieldModal();

}

function getSelectedString() {
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

