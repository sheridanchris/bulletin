function setPage(page) {
    if ('URLSearchParams' in window) {
        var searchParams = new URLSearchParams(window.location.search);
        searchParams.set("page", page);
        window.location.search = searchParams.toString();
    }
}
