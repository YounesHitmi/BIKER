class Navbar extends HTMLElement {
    connectedCallback(){
        const isHome = window.location.pathname.includes("homepage");
        const bgColor = isHome ? "rgba(0, 0, 0, 0.6)" : "rgba(255, 255, 255, 0.44)";   
        const borderColor = isHome ? "transparent" : "#cccccc";
        const textColor = isHome ? "white" : "black";

        this.innerHTML = `
        <style>
            @font-face {
                font-family: 'Nechlas';
                src: url('../resources/fonts/Nechlas.ttf') format('opentype');
                font-weight: normal;
                font-style: normal;
            }

            header {    
                width: 90%;
                margin-left: auto;
                margin-right: auto;
                margin-top: 1vh;
                height: fit-content;
                display: flex;
                flex-direction: row;
                text-align: center;
                font-weight: bold;
                width: 100%; /* prend toute la largeur du host */
                border-bottom: ${borderColor} solid 1px;
                border-radius: 15px;
                background-color: ${bgColor};
                backdrop-filter: blur(3px);
            }

            #logo {
                height: 50px;
                width: auto;
                margin: 10px;
            }


                nav {
                    display: flex;
                    flex-direction: row;
                    width: 100%;
                    height: 100%;
                    align-items: center;
                    justify-content: space-evenly;
                    margin : auto;
                }

            nav a {
                font-family: 'Poppins';
                font-size: 1rem;
                color: ${textColor};
                text-decoration: none;
                color: white;
                transition: transform 0.3s ease, box-shadow 0.3s ease;
            }

            nav a:hover {
                color: #52fb72;
                transform: scale(1.2);
            }

        </style>
        <header>
            <img id="logo" src="../logopns.png" alt="logopns">
            <nav>
                <a href="../homepage/homepage.html">HOME</a>
                <a href="../itinerary/itinerary.html">ITINERARY</a>
                <a href="../About%20us/aboutus.html">ABOUT US</a>
            </nav>
        </header>
    `;
    }
}
customElements.define('main-navbar', Navbar);