let dotNetRef;

window.initializeGoogleSignIn = function(dotnetHelper) {
    dotNetRef = dotnetHelper;

    function waitForGoogle() {
        if (window.google && google.accounts && google.accounts.id) {
            // Google library is ready
            google.accounts.id.initialize({
                client_id: "655071828601-0sna2oihnj5ieu52ud95t3buht58v4sh.apps.googleusercontent.com",
                callback: handleCredentialResponse
            });

            function waitForButton() {
                const buttonContainer = document.getElementById("googleSignInButton");
                if (buttonContainer) {
                    google.accounts.id.renderButton(buttonContainer, {
                        theme: "outline",
                        size: "large",
                        locale: "el-GR"
                    });
                } else {
                    setTimeout(waitForButton, 50);
                }
            }

            waitForButton();
        } else {
            // Retry after 50ms
            setTimeout(waitForGoogle, 50);
        }
    }

    waitForGoogle();
};


window.triggerGoogleSignIn = function() {
    google.accounts.id.prompt();
}

function handleCredentialResponse(response) {
    const idToken = response.credential;
    dotNetRef.invokeMethodAsync('HandleGoogleLogin', idToken);
}
