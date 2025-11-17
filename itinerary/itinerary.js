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

document.addEventListener('DOMContentLoaded', () => {
    const storedData = localStorage.getItem('itineraryResult');

    if(!storedData) {
        document.querySelector('.road').innerHTML = "Aucun itinéraire n'a été calculé. <a href='../homepage/homepage.html'>Retour à l'accueil</a>";
        return;
    }

    const data = JSON.parse(storedData);

    localStorage.removeItem('itineraryResult');

    drawMap(data);
});

function drawMap(data) {
    console.log("Contenu de data.Segments:", data.Segments);
    const map = L.map('map');

    L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">'
    }).addTo(map);

    const walkIcon = L.icon({
        iconUrl: '../resources/ICONE_MARCHE.png',
        iconSize: [32, 37],
        iconAnchor: [16, 37],
    });

    const bikeIcon = L.icon({
        iconUrl: '../resources/ICONE_VELO.png',
        iconSize: [32, 37],
        iconAnchor: [16, 37],
    });

    const routeCoordinates = [];

    data.Segments.forEach((segment, index) => {
        let geometryObject;
        if (segment.Geometry && typeof segment.Geometry === 'string') {
            try {
                geometryObject = JSON.parse(segment.Geometry);
            } catch (e) {
                console.error("Erreur lors de l'analyse de la géométrie JSON:", e);
                return;
            }
        }

        if (geometryObject && geometryObject.coordinates && geometryObject.coordinates.length > 0) {
            const lngLatArray = geometryObject.coordinates;
            const latLngs = lngLatArray.map(coord => [coord[1], coord[0]]); //On inverse pour Leaflet qui prend long,lat
            routeCoordinates.push(...latLngs);

            let color;
            let dashArray = null;
            const startPoint = latLngs[0];


            if (index === 0) {
                color = 'orange';
                dashArray = '5, 10';
                L.marker(startPoint, { icon: walkIcon }).addTo(map);
            } else if (index === 1) {
                color = 'green';
                dashArray = null;
                L.marker(startPoint, { icon: bikeIcon }).addTo(map);
            } else if (index === 2) {
                color = 'orange';
                dashArray = '5, 10';
                L.marker(startPoint, { icon: walkIcon }).addTo(map);
            }

            L.polyline(latLngs, { color: color, weight: 5, dashArray : dashArray }).addTo(map);
        }
    });

    if (routeCoordinates.length > 0) {
        const bounds = L.latLngBounds(routeCoordinates);
        map.fitBounds(bounds, { padding: [50, 50] });
    } else {
        console.error("Aucune géométrie valide trouvée dans les données.");
        map.setView([45.76, 4.83], 13);
    }
}


