const debounce = (func, delay) => {
    let timer;
    return function (...args) {
        clearTimeout(timer);
        timer = setTimeout(() => func.apply(this, args), delay);
    };
};

class Itinerary extends HTMLElement {
    connectedCallback(){
        this.innerHTML= `
          <style>
            #searchButton {
                background-color: #51f770;
                background: linear-gradient(135deg, #51f770, #28ba65ff);
                color: #03001d;
                border: none;
                border-radius: 999px;
                cursor: pointer;
                font-size: 1rem;
                transition: transform 0.3s ease, box-shadow 0.3s ease;
                padding: 1rem;
                font-family: 'Poppins', sans-serif;
                width: fit-content;
                margin-top: 2vh;
                box-shadow: 0 8px 15px rgba(0, 0, 0, 0.2);
            }
            
            #searchButton:hover {
                transform: translateY(-2px);
                box-shadow: 0 10px 20px rgba(0, 0, 0, 0.3);
            }
            
            .input {
              display: flex;
              flex-direction: column;
              align-items: center;
              gap: 2vh;
            }

            input{
                background-color: #f2f4f7;
            }
            
            #start, #end{
              position: relative;
            }
            
            #start, #end{
              width: 10vw;
              padding: 1rem;
              border-radius: 10px;
              border: 1px solid gray;
              opacity: 0.7;
              transition: transform 0.3s ease, box-shadow 0.3s ease;
            }
            
            h4{
            margin-bottom: 0;
            }
            
            #start:hover, #end:hover {
              transform: scale(1.02);
              box-shadow: 0 10px 20px rgba(0, 0, 0, 0.3);
            }

            ul:empty {
              display: none;
            }

            ul {
              position: absolute;
              top: 100%; 
              left: 0;
              width: 100%;
              list-style: none;
              padding: 0;
              margin: 0;
              background: white;
              border: 1px solid #ccc;
              max-height: 200px;
              overflow-y: auto;
              z-index: 999;
            }
            
            li {
              padding: 0.5rem;
              cursor: pointer;
            }
            li:hover {
              background-color: #f0f0f0;
            }
          </style>
       
        <div class="input">
            <h4>Départ</h4>
            <input id="start">
            <ul id="start-suggestions"></ul>
            <h4>Arrivée</h4>
            <input id="end">
            <ul id="end-suggestions"></ul>
          <button id="searchButton" type="button">Rechercher</button>
          <div id="itinerary-result"></div>
        </div>
        `;


        this.querySelector('#searchButton').addEventListener('click', () => {
            this.fetchItinerary();
        });

        const startInput = this.querySelector("#start");
        const endInput = this.querySelector("#end");
        const startList = this.querySelector("#start-suggestions");
        const endList = this.querySelector("#end-suggestions");

        startInput.addEventListener("input", debounce(() => {
            this.fetchSuggestions(startInput, startList);
        }, 150));

        endInput.addEventListener("input", debounce(() => {
            this.fetchSuggestions(endInput, endList);
        }, 150));
    }

    fetchItinerary(){
        const origin = this.querySelector("#start").value;
        const destination = this.querySelector("#end").value;
        const resultDiv = this.querySelector("#itinerary-result");

        resultDiv.innerText = "Calcul en cours...";
        resultDiv.style.color = "black";


        const url = `http://localhost:8734/RoutingServer/itinerary?` +
            `from=${encodeURIComponent(origin)}&` +
            `to=${encodeURIComponent(destination)}`;

        fetch(url).then(response => {
            if (!response.ok) {
                throw new Error(`Erreur HTTP: ${response.status}`);
            }
            return response.json();
        }).then(data => {
            if (data.Status.includes("ERREUR")) {
                resultDiv.innerText = data.Message;
                resultDiv.style.color = "red";
            }
            else{
                localStorage.setItem("itineraryResult", JSON.stringify(data));
                window.location.href = "../itinerary/itinerary.html";
            }
        }).catch(error => {
            console.error("Erreur Fetch:", error);
            resultDiv.style.color = "red";
            resultDiv.innerText = "Erreur de connexion avec le serveur de routage.\n" +
                "Vérifiez que vos serveurs sont lancés (en admin) et que le CORS est activé.";
        });
    }

    async fetchSuggestions(input, list){
        const query = input.value.trim();

        if (query.length < 3) {
            list.innerHTML = "";
            return;
        }

        list.innerHTML = "<li>Chargement...</li>";

        const url = `https://api-adresse.data.gouv.fr/search/?q=${encodeURIComponent(query)}&limit=15`;

        try{
            const response = await fetch(url);
            const data = await response.json();

            list.innerHTML = "";

            data.features.forEach((feature) => {
                const li = document.createElement("li");
                li.textContent = feature.properties.label;

                li.addEventListener("click", () => {
                    input.value = feature.properties.label;
                    list.innerHTML = "";
                });
                list.appendChild(li);
            });
        }
        catch(error) {
            console.error("Erreur lors de la récupération des suggestions d'adresse:", error);
            list.innerHTML = "<li>Erreur de chargement...</li>";
        }

    }
}
customElements.define('itinerary-component', Itinerary);



