function setPage(page) {
    if ('URLSearchParams' in window) {
        var searchParams = new URLSearchParams(window.location.search);
        searchParams.set("page", page);
        window.location.search = searchParams.toString();
    }
}

const showReplies = document.querySelectorAll('.commentReplies')

showReplies.forEach(btn => btn.addEventListener('click', (e) => {
    let parentContainer = e.target.closest('.comment-container')
    let _id = parentContainer.id

    if(_id) {
        let childrenContainer = parentContainer.querySelectorAll(`[dataset=${_id}]`)

        childrenContainer.forEach(child => child.classList.toggle('opened'))
    }
}))
