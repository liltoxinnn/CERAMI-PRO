// Lecture des codes par la caméra de l'appareil.
//
// Les navigateurs récents (Chrome, Edge, Android) savent lire les codes QR et
// les codes-barres sans bibliothèque externe grâce à « BarcodeDetector ».
// Quand ce n'est pas disponible, l'écran bascule sur la saisie manuelle ou la
// douchette USB : la fonctionnalité reste utilisable partout.

window.ceramipro = window.ceramipro || {};

(() => {
    let flux = null;
    let detecteur = null;
    let minuterie = null;

    const formats = ['qr_code', 'code_39', 'code_128', 'ean_13', 'ean_8'];

    window.ceramipro.cameraDisponible = () =>
        'BarcodeDetector' in window &&
        !!navigator.mediaDevices &&
        typeof navigator.mediaDevices.getUserMedia === 'function';

    window.ceramipro.demarrerCamera = async (idVideo, objetDotNet) => {
        if (!window.ceramipro.cameraDisponible()) {
            return "Cet appareil ne sait pas lire les codes avec la caméra.";
        }

        const video = document.getElementById(idVideo);
        if (!video) {
            return "L'aperçu de la caméra est introuvable.";
        }

        try {
            const disponibles = await window.BarcodeDetector.getSupportedFormats();
            detecteur = new window.BarcodeDetector({
                formats: formats.filter((f) => disponibles.includes(f))
            });

            flux = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: 'environment' }
            });

            video.srcObject = flux;
            await video.play();
        } catch (erreur) {
            window.ceramipro.arreterCamera(idVideo);
            return "La caméra n'a pas pu être ouverte. Vérifiez l'autorisation demandée par le navigateur.";
        }

        minuterie = setInterval(async () => {
            if (!detecteur || !video.videoWidth) {
                return;
            }

            try {
                const codes = await detecteur.detect(video);
                if (codes && codes.length > 0 && codes[0].rawValue) {
                    const valeur = codes[0].rawValue;
                    window.ceramipro.arreterCamera(idVideo);
                    await objetDotNet.invokeMethodAsync('CodeLu', valeur);
                }
            } catch {
                // Une image illisible n'est pas une erreur : on réessaie.
            }
        }, 400);

        return null;
    };

    window.ceramipro.arreterCamera = (idVideo) => {
        if (minuterie) {
            clearInterval(minuterie);
            minuterie = null;
        }

        if (flux) {
            flux.getTracks().forEach((piste) => piste.stop());
            flux = null;
        }

        detecteur = null;

        const video = document.getElementById(idVideo);
        if (video) {
            video.srcObject = null;
        }
    };

    // Place le curseur dans le champ de saisie : la douchette USB tape le code
    // puis valide, exactement comme un clavier.
    window.ceramipro.focus = (id) => {
        const champ = document.getElementById(id);
        if (champ) {
            champ.focus();
            champ.select();
        }
    };
})();
