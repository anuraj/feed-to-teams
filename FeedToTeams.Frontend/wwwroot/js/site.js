var notyf = new Notyf({
    duration: 2000,
    position: {
        x: 'right',
        y: 'top',
    },
    dismissible: true
});

$(document).ready(function () {
    $("#channelsLoading").hide();
    $.ajax({
        url: GetJoinedTeamsEndpoint,
        type: "GET",
        beforeSend: function () {
            $("#teamsLoading").show();
        },
        success: function (data) {
            var select = $("#teams");
            select.empty();
            select.append($("<option></option>").attr("value", "").attr("selected", "").text("Select team"));
            $.each(data, function (index, value) {
                select.append($("<option></option>").attr("value", value.id).text(value.name));
            });
            $("#teamsLoading").hide();
        },
        error: function (xhr, status, error) {
            console.log(xhr.responseText);
            $("#teamsLoading").hide();
        }
    });

    $('#teams').change(function () {
        $("#channelsLoading").show();
        var select = $("#channels");
        select.empty();
        select.append($("<option></option>").attr("value", "").attr("selected", "").text("Select channel"));
        var teamId = $(this).val();
        $.ajax({
            url: GetChannelsByIdEndpoint + "?id=" + teamId,
            type: "GET",
            beforeSend: function () {
                $("#channelsLoading").show();
            },
            success: function (data) {
                $.each(data, function (index, value) {
                    select.append($("<option></option>").attr("value", value.id).text(value.name));
                });
                $("#channelsLoading").hide();
            },
            error: function (xhr, status, error) {
                console.log(xhr.responseText);
                $("#channelsLoading").hide();
            }
        });
    });

    var validator = $("#CreateForm").validate({
        errorClass: 'text-danger',
        validClass: 'text-success',
        errorElement: 'span',
        highlight: function (element, errorClass, validClass) {
            $(element).addClass("is-invalid").removeClass("is-valid");
        },
        unhighlight: function (element, errorClass, validClass) {
            $(element).removeClass("is-invalid").addClass("is-valid");
        },
        rules: {
            rssurl: "required",
            teams: "required",
            channels: "required"
        },
        messages: {
            rssurl: "Please enter a valid RSS feed URL",
            teams: "Please select a team",
            channels: "Please select a channel"
        },
        submitHandler: function (form) {
            var RSSFeedURL = $("#rssurl").val();
            var teamId = $("#teams").val();
            var channelId = $("#channels").val();
            var teamName = $("#teams option:selected").text();
            var channelName = $("#channels option:selected").text();
            $.ajax({
                url: CreateFeedEndpoint,
                type: "POST",
                data: {
                    RSSFeedURL: RSSFeedURL,
                    TeamId: teamId,
                    ChannelId: channelId,
                    TeamName: teamName,
                    ChannelName: channelName
                },
                success: function (data) {
                    notyf.open({
                        duration: 3000,
                        type: 'success',
                        message: 'Feed configured successfully.',
                        dismissible: true
                    });
                    $("#CreateForm").trigger("reset");
                    var select = $("#channels");
                    select.empty();
                    select.append($("<option></option>").attr("value", "").attr("selected", "").text("Select channel"));
                    validator.resetForm();
                    getFeeds();
                },
                error: function (xhr, status, error) {
                    console.log(xhr.responseText);
                    notyf.open({
                        duration: 3000,
                        type: 'error',
                        message: 'Unable to configure the feed. Please retry after sometime.',
                        dismissible: true
                    });
                    validator.resetForm();
                }
            });
        }
    });

    getFeeds();
});

var sendFeed = function (id) {
    $.ajax({
        url: SendFeedEndpoint,
        type: "POST",
        data: {
            id: id
        },
        success: function (data) {
            notyf.open({
                duration: 3000,
                type: 'success',
                message: 'Feed sent successfully.',
                dismissible: true
            });
        }
    });
}

var deleteFeed = function (id) {
    alert('Not implemented yet.')
}

var getFeeds = function () {
    $.ajax({
        url: GetFeedsEndpoint,
        type: "GET",
        beforeSend: function () {
            $("#feedsLoading").show();
        },
        success: function (data) {
            var feedList = $("#feedList");
            feedList.html("");
            if (data.length !== 0) {
                $.each(data, function (index, value) {
                    var html = "<a href=\"" + value.rssFeedURL + "\" target=\"_blank\">" + value.rssFeedURL
                        + "</a><br /> Team :" + value.teamName + "<br />Channel : " + value.channelName
                        + "<button class=\"btn btn-danger btn-sm float-end\" onclick=\"deleteFeed('" + value.id + "')\">Delete</button>"
                        + "<button class=\"btn btn-primary btn-sm float-end me-2\" onclick=\"sendFeed('" + value.id + "')\">Send latest feed to Channel</button>";
                    //+ "<br /><small>Last published URL : " + value.lastPublishedUrl + "<br />Last published date : " + value.publishedOn + "</small>";
                    feedList.append($("<div></div>").attr("class", "list-group-item list-group-item-action").html(html));
                });
            } else {
                feedList.append($("<div></div>").attr("class", "list-group-item list-group-item-action").html("<h4>No feeds configured yet.</h4><a data-bs-toggle=\"offcanvas\" href=\"#offcanvasCreateForm\">Click here to configure a feed</a>"));
            }
            $("#feedsLoading").hide();
        },
        error: function (xhr, status, error) {
            console.log(xhr.responseText);
            $("#feedsLoading").hide();
        }
    });
}