class Navbar extends HTMLElement {
    connectedCallback(){
        this.innerHTML= `   
            <style>
                header{
                    border-bottom: #cccccc solid 1px;
                    border-radius: 15px;
                    background-color: rgba(241, 241, 241, 0.5);
                }
            </style>
            <header style="
                text-align: center;
                font-weight: bold;
                width: 100%;
            ">                
                <img id="logo" src="../logopns.png" alt="logopns">                
                <nav>                    
                    <a href="../homepage/homepage.html">Home</a>                    
                    <a href="../itinerary/itinerary.html">Itinerary</a>                   
                    <a href="../About%20us/aboutus.html">About us</a>                               
                </nav>            
            </header>`;
    }
}
customElements.define('main-navbar', Navbar);