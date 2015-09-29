// Ready will call page numbers generator and will adjust to last page.
// Last page is different because of the way it calls the last page information.
$(document).ready(function () {
    var pageWeOn2 = $("#currentPage").attr("value");
    var totalPages2 = $("#maxPages").attr("value");
    var isThisLastPage = $("#isLast").attr("value");

    if (isThisLastPage == 1) { //Last page needs special care because of the Amazon limit.
        $('#firstPage').hide();
        $('#secondPage').show();
        $('#currentPage').attr("value", pageWeOn2 + 1);
        var valueFor = parseInt(pageWeOn2) + 1;
        $('#currentPage').text(valueFor);
    } else {
        $('#secondPage').hide();
    }

    GeneratePager();
});

//This updates the currencies. It makes AJAX call to the controller.
function UpdateStuff() {
    var e = document.getElementById("CurrencyDropDown");
    var strUser = e.options[e.selectedIndex].text;

    $.ajax({
        cache: false,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: 'json',
        url: "/Home/GetExchangeRate",
        data: { 'Currency': strUser },
        async: true,
        success: function (data) {

            $("#num").html(data);
            var divs = document.getElementsByClassName("price");
            for (var i = 0; i < divs.length; i++) {
                if (divs[i].getAttribute('value') >= 0) {
                    divs[i].innerHTML = ((parseFloat(data).toPrecision(4) * parseFloat(divs[i].getAttribute('value')).toPrecision(4)) / 100).toFixed(2);
                } else {
                    divs[i].innerHTML = "X";
                }
            }
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert("Ajax Failed!!! Panic!");
        }
    });
}

// The difference between last page with search and regular with search is that if it
// needs to make the extra queries for next page.
function SelectPageWithSearch(pageNumber, searchTerm) {
    console.log("calling select page with search");
    var url = "/Home/Search?page=" + pageNumber + "&searchTerm=" + searchTerm + "&isLast=0";
    window.location.href = url;
}

function SelectPageWithSearchLastPage(pageNumber, searchTerm, spotInList) {
    console.log("calling select page with search");
    var url = "/Home/Search?page=" + (pageNumber - 1) + "&searchTerm=" + searchTerm + "&isLast=1";
    window.location.href = url;
}

function MakeAjaxGet(page, searchTerm, divToUpdateYo) {
    $.ajax({
        url: '/Home/ajaxNewPage',
        data: { "page": page, "searchTerm": searchTerm, "divToUpdate": divToUpdateYo },
        type: 'GET',

        success: function (partialView) {
            $('#firstPage').html(partialView);
        }
    });
}

// Will select the next page that has been preloaded.
function SelectNextPage(isEven, isLast, pageComingFrom, spotOfCaller, spotOfLast) {

    var searchWord2 = $("#searchWord").text();
    var nextPage = pageComingFrom + 2;
    $('#currentPage').attr("value", pageComingFrom + 1);
    GeneratePager();

    // Since I'm using 2 divs to populate in turns it will choose different one to 
    // update.
    if (isEven == true) {
        $(document).ready(function () {
            console.log("firstPageShouldAppear");
            $('#secondPage').hide();
            $('#firstPage').show();
            console.log("showing first, hiding second")
            if (isLast == false) {
                MakeAjaxGet(nextPage, searchWord2, "secondPage");
                console.log("madeajaxcalloneven")    
                var spotOfNext = spotOfCaller + 1;
                if (spotOfNext == spotOfLast) {
                    $('#Page' + spotOfNext).unbind();
                    $('#Page' + spotOfNext).click(function () {
                        SelectNextPage(false, true, pageComingFrom++, nextPage, spotOfLast)
                    });
                } else {
                    $('#Page' + spotOfNext).unbind();
                    $('#Page' + spotOfNext).click(function () {
                        SelectNextPage(false, false, pageComingFrom++, nextPage, spotOfLast)
                    });
                }
            }
        });

    } else {
        $(document).ready(function () {
            console.log("secondPageShouldAppear");
            $('#secondPage').show();
            $('#firstPage').hide();
            console.log("showing second, hiding first")
            if (isLast == false) {
                MakeAjaxGet(nextPage, searchWord2, "firstPage");
                console.log("madeajaxcallonuneven");
                var spotOfNext = spotOfCaller + 1;
                if (spotOfNext == spotOfLast) {
                    $('#Page' + spotOfNext).unbind();
                    $('#Page' + spotOfNext).click(function () {
                        SelectNextPage(true, true, pageComingFrom++, nextPage, spotOfLast)
                    });
                } else {
                    $('#Page' + spotOfNext).unbind();
                    $('#Page' + spotOfNext).click(function () {
                        SelectNextPage(true, false, pageComingFrom++, nextPage, spotOfLast)
                    });
                }
            }
        });
    }

    var spotOfPrevious = spotOfCaller - 1;

    $('#Page' + spotOfPrevious).unbind();
    $('#Page' + spotOfPrevious).click(function () {
        SelectPageWithSearch(pageComingFrom, searchWord2);
    });
    
}

//Generates the page numbers.
function GeneratePager() {

    var searchWord2 = $("#searchWord").text();
    $(".searchBoxBox").val(searchWord2);
    var page = parseInt($("#currentPage").attr("value"));
    var searchWord = $("#searchWord").text();
    var totalPages = parseInt($("#maxPages").attr("value"));

    $(document).ready(function () {
        if (totalPages > 1) {
            $(".PageShortcut").each(function (i, obj) {
                // Hide the link refs that are higher than necessary.
                if (i >= totalPages) {  
                    $(obj).hide();
                } else {
                    // If there are less or equal than 7 pages or we are on the first 4 pages.
                    if (totalPages <= 7 || page <= 4) {     
                        var pageNumberForDiv = (1 + i);
                        var spotOfLastPage = null;
                        // Only check for 7 or less because the 4 page rule wont get into this thread.
                        if (totalPages <= 7) {
                            // If there are more than 7 and we're on >4 page.
                            spotOfLastPage = totalPages;                
                        }

                        // Populate the numbers.
                        $(obj).text(pageNumberForDiv);  
                        // If click is on the page number we are on, do nothing.
                        if (i + 1 == page) { 
                            $(obj).unbind();
                            $(obj).click(function () {
                                console.log("already here");
                            });
                        // If the element refers to a page that is not current page  
                        } else {
                            // Check if the current element in list is the next from page we are on.
                            if (i + 1 == page + 1) {
                                // Check if next page is last in list.
                                if (page + 1 < totalPages) {     
                                    // If not then assign select page functions without islast bool true.
                                    // Checking this to know if I should preload the next page.
                                    if ((page + i + 1) % 2 == 0) {
                                        $(obj).unbind();
                                        $(obj).click(function () {
                                            SelectNextPage(true, false, page, i + 1, spotOfLastPage);
                                        });
                                    } else {
                                        $(obj).unbind();
                                        $(obj).click(function () {
                                            SelectNextPage(false, false, page, i + 1, spotOfLastPage);
                                        });
                                    }
                               // If next is last, assign preload functions.
                               }else if (page + 1 == totalPages || page + 1 == 7) { 
                                    if ((page + i + 1) % 2 == 0) {
                                        $(obj).unbind();
                                        $(obj).click(function () {
                                            SelectNextPage(true, true, page, i + 1, spotOfLastPage);
                                        });

                                    } else {
                                        $(obj).unbind();
                                        $(obj).click(function () {
                                            SelectNextPage(false, true, page, i + 1, spotOfLastPage);
                                        });
                                    }
                                }
                            }
                            else if (i + 1 == totalPages) {
                                $(obj).click(function () {
                                    SelectPageWithSearchLastPage(i + 1, searchWord, i + 1);
                                });

                            } else {
                                $(obj).click(function () {
                                    SelectPageWithSearch(i + 1, searchWord);
                                });
                            }
                        }


                    } else { // Here are cases when the middle has to start flaoting in a longer list.
                             // Since amazon doesn't allow to query more than 5 pages I'll leave this for future.
                        $(obj).text(page - 3 + i);
                    }
                }
            });
        // There is only one page so hide every page except for 1.
        } else { 
            $(".PageShortcut").each(function (i, obj) {
                if (i != 0) {
                    $(obj).hide();
                } else {
                    $(obj).unbind();
                    $(obj).text(page + i);
                }
            });
        }
    });
}