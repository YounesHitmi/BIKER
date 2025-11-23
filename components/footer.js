class Footer extends HTMLElement {
    connectedCallback(){
        this.innerHTML= `
            <style>
            footer{
                border-top-left-radius: 15px;
                border-top-right-radius: 15px;
            }
            </style>
            <footer style="
                background-color: #52fb71ff;
                padding: 3.5vh;
                text-align: center;
                font-weight: bold;
                width: 100%;
            ">
                Younes Hitmi pour Polytech Nice Sophia - 2025
            </footer>
        `;
    }
}
customElements.define('main-footer', Footer);
