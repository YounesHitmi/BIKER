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



