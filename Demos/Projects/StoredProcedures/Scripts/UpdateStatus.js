function updateStatus() {
    var collection = getContext().getCollection();

    collection.queryDocuments(
        collection.getSelfLink(),
        'SELECT * FROM statuses s where s.retweet_count > 900',
        function (err, feed, options) {
            if (err) throw err;

            if (!feed || !feed.length) getContext().getResponse().setBody("no docs found");
            else feed.forEach(function (status) {
                    status.user.followers_count = 0;
                    var isAccepted = collection.replaceDocument(status._self, status, function (err) {
                        if (err) throw err;
                    });
                    if (!isAccepted) throw new Error("The call replaceDocument(status) returned false.");
                    else getContext().getResponse().setBody("Script executed");
                });
        });
}