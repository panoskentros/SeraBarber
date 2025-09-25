let dotNetRef;

window.initializeGoogleSignIn = function(dotnetHelper) {
    dotNetRef = dotnetHelper;

    google.accounts.id.initialize({
        client_id: "655071828601-0sna2oihnj5ieu52ud95t3buht58v4sh.apps.googleusercontent.com",
        callback: handleCredentialResponse
    });

    function waitForButton() {
        const buttonContainer = document.getElementById("googleSignInButton");
        if (buttonContainer) {
            google.accounts.id.renderButton(buttonContainer, { theme: "outline", size: "large" });
        } else {
            // Retry after 50ms
            setTimeout(waitForButton, 50);
        }
    }

    waitForButton();
};

window.triggerGoogleSignIn = function() {
    google.accounts.id.prompt();
}

function handleCredentialResponse(response) {
    const idToken = response.credential;
    dotNetRef.invokeMethodAsync('HandleGoogleLogin', idToken);
}
