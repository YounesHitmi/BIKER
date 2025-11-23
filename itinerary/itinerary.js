const burger = document.querySelector('.mBurger');
const navbar = document.querySelector('main-navbar');
navbar.classList.add("main-navbar");

burger.addEventListener('click', () => {
    burger.classList.toggle('cross');
    navbar.classList.toggle('mobile');
});

navbar.querySelectorAll("nav a").forEach(n => {
    n.addEventListener('click', () => {
        burger.classList.remove('cross');
        navbar.classList.remove('mobile');
    });
});

const typeIcons = {
    0: "../resources/arrows/left.png",
    1: "../resources/arrows/right.png",
    6: "../resources/arrows/forward.png",
    9: "../resources/arrows/u-turn.png",
    default: "../resources/arrows/start.png",
};

document.addEventListener('DOMContentLoaded', () => {
    connectToNotifications();
    const storedData = localStorage.getItem('itineraryResult');

    if(!storedData) {
        document.querySelector('.road').innerHTML = "Aucun itinéraire n'a été calculé. <a href='../homepage/homepage.html'>Retour à l'accueil</a>";
        return;
    }

    const data = JSON.parse(storedData);

    localStorage.removeItem('itineraryResult');
    displayInstructions(data);
    displayDetails(data);
    drawMap(data);
    
});

function drawMap(data) {
    const map = L.map('map');

    L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">'
    }).addTo(map);

    const walkIcon = L.icon({
        iconUrl: '../resources/ICONE_MARCHE.png',
        iconSize: [20, 23],
        iconAnchor: [10, 23],
    });

    const bikeIcon = L.icon({
        iconUrl: '../resources/ICONE_VELO.png',
        iconSize: [20, 23],
        iconAnchor: [10, 23],
    });

    const routeCoordinates = [];

    data.Segments.forEach((segment) => {

        if (!segment.Geometry) return;

        let latLngs;

        try {
            latLngs = polyline.decode(segment.Geometry); 
        } catch (e) {
            console.error("Erreur de décodage polyline:", e);
            return;
        }

        routeCoordinates.push(...latLngs);

        let options = {
            color: segment.Profile === 'cycling-regular' ? 'blue' : 'green',
            weight: 5,
            opacity: 0.7,
            dashArray: segment.Profile === 'cycling-regular' ? null : '10, 15'
        };

        L.polyline(latLngs, options).addTo(map);
        L.marker(latLngs[0], { icon: segment.Profile === 'cycling-regular' ? bikeIcon : walkIcon }).addTo(map);

        });


    if (routeCoordinates.length > 0) {
        const bounds = L.latLngBounds(routeCoordinates);
        map.fitBounds(bounds, { padding: [50, 50] });
    } else {
        console.error("Aucune géométrie valide trouvée dans les données.");
        map.setView([45.76, 4.83], 13);
    }
}

function displayInstructions(data){
    const boxDetails = document.querySelector('.box-details');

    boxDetails.innerHTML = '';
    data.Segments.forEach((segment, index)=>{
        const sectionTitle = document.createElement('h4');
        
        sectionTitle.classList.add('sectionTitle');

        if (segment.Profile === 'cycling-regular') {
            sectionTitle.textContent = "🚴 Trajet à Vélo";
            sectionTitle.style.color = "#494cfb";
            sectionTitle.style.backgroundColor = "#494cfb4b";
        } else {
            sectionTitle.textContent = "🚶 Trajet à Pied";
            sectionTitle.style.color = "#4ca93e";
            sectionTitle.style.backgroundColor = "rgba(80, 167, 67, 0.28)";
        }
        boxDetails.appendChild(sectionTitle);

        if (segment.Instructions && segment.Instructions.length > 0) {
            segment.Instructions.forEach(step => {
                const stepElement = document.createElement('h4');
                stepElement.style.display = "flex";
                stepElement.style.flexDirection = "row";
                stepElement.style.gap = "1.5vw";

                let iconUrl = typeIcons[step.Type] || typeIcons.default;

                if (step.Description.includes("gauche") && !step.Description.includes("Arrivé")) iconUrl = typeIcons[0];
                else if (step.Description.includes("droite") && !step.Description.includes("Arrivé")) iconUrl = typeIcons[1];

                const img = document.createElement('img');
                img.src = iconUrl;
                img.style.width = "20%";
                img.classList.add("instruction-icon");

                const dist = Math.round(step.Distance);
                let distanceText = dist > 0 ? ` dans ${dist} m` : '';
                if(step.Type === 6){
                    distanceText = dist > 0 ? ` sur ${dist} m` : '';
                }
                const text = document.createElement('span');
                text.textContent = `${step.Description}${distanceText}`;

                stepElement.appendChild(img);
                stepElement.appendChild(text);
                boxDetails.appendChild(stepElement);
            });
        } else {
            const noStep = document.createElement('h4');
            noStep.textContent = "Suivez le tracé sur la carte.";
            boxDetails.appendChild(noStep);
        }
    })
}

function displayDetails(data){
    const timeSection = document.querySelector('#time');
    const distanceSection = document.querySelector('#distance');
    const meanSection = document.querySelector('#moyen');
    const comparisonSection = document.querySelector('#comparaison');
    const stepSection = document.querySelector('#etapes');
    const listDetails = document.querySelector('.list-details');

    timeSection.textContent = "🕑 Temps de trajet : "+ data.Time + " min.";
    distanceSection.textContent = "↔️ Distance totale à parcourir : "+ data.Distance + " m.";
    comparisonSection.textContent = data.Comparison;
    stepSection.textContent = data.Steps;
}

function connectToNotifications() {
    console.log('Connection aux notifications ActiveMQ');
    const url = "ws://localhost:61614/stomp";
  
    const client = Stomp.client(url);

    client.debug = null; 

    client.connect({}, function (frame) {
        console.log('Connecté aux notifications ActiveMQ');

        client.subscribe('/topic/BikingEvents', function (message) {
            try {
                if (message.body) {
                    console.log(message.body);
                    const event = JSON.parse(message.body);
                    showPopup(event);
                }
            } catch (e) {
                console.error("Erreur lecture notif:", e);
            }
        });
    }, function(error) {
        console.log("ActiveMQ non détecté ou erreur de connexion:", error);
    });
}

function showPopup(event) {
    let container = document.getElementById('notification');
    if (!container) {
        container = document.createElement('div');
        container.id = 'notification';
        document.body.appendChild(container); 
    }
    
    const toast = document.createElement('div');
    toast.classList.add('notification-toast');
    
    let icon = "📢";
    if (event.type === "Meteo") icon = "⛈️";
    if (event.type === "Pollution") icon = "🫁";
    if (event.type === "InfoTrafic") icon = "🚗";

    toast.innerHTML = `
        <div style="font-weight:bold; margin-bottom:5px;">${icon} ${event.type}</div>
        <div>${event.message}</div>
    `;

    if (event.level === "Critique") {
        toast.style.backgroundColor = "#e74c3c"; 
    } else if (event.level === "Avertissement") {
        toast.style.backgroundColor = "#f39c12"; 
        toast.style.color = "white";
    } else {
        toast.style.backgroundColor = "#2ecc71"; 
    }

    toast.addEventListener('click', () => {
        removeToast(toast);
    });

    container.appendChild(toast);

    setTimeout(() => {
        removeToast(toast);
    }, 8000);
}

function removeToast(toast) {
    toast.classList.add('notification-hide');
    toast.addEventListener('animationend', () => {
        toast.remove();
    });
}
