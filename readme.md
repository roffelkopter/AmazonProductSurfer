# AmazonProductSurfer

Põhilised koodi osad:

Controller - https://github.com/roffelkopter/AmazonProductSurfer/blob/master/Amazon%20Product%20Surfer/Controllers/HomeController.cs

Models - https://github.com/roffelkopter/AmazonProductSurfer/tree/master/Amazon%20Product%20Surfer/Models

JS - https://github.com/roffelkopter/AmazonProductSurfer/blob/master/Amazon%20Product%20Surfer/Scripts/AmazeSurferJS.js

Ülesanne:

Lahendada .NET'is ja soovitatavalt MVC-raamistikku kasutades. Info presenteerimine võiks olla JQuery’t kasutades.

1) Tee veebilehekülg, mille kaudu on võimalik otsida tooteid Amazon’i toodete hulgast.

2) Kasutades Amazon’i Product Advertising API (võib ka REST-i ) teenuseid, päri Amazon’ist otsingu sõna alusel toodete nimekiri.

3) Näita tulemusi 13 kaupa lehel koos hinnaga. Järgmine vastuste leht „preloaded“.

4) Kasutades valuutakursi kalkulaator teenust võimalda kasutajal valida endale sobivaim valuutakurss ja näita tulemusi selle kursiga.

5) Hinna/valuuta muutus realiseeri AJAX’iga update’iga mitte full page load’iga.
 
Tee nii, et leht näeks natukenegi ilus välja ka.

Lahendus:

AJAX-it kasutasin valuutakursside muutmisel ja järgmise lehekülje preloadimiseks, kui käesolev leht oli ise juba preloaded. 

Dünaamiline preloadimine töötab hetkel ainult järgnevate lehtede suunas. Tagasiminemine eelmisele ja kõik ülejäänud, mis pole kohe järgmine leht, töötavad full page reloadiga. 

Otsingutulemusi kuvab lehekülje peale 13, aga ainult maksimaalselt kolme lehekülje jagu, kuna Amazon piirab päringuid 50 tulemusega otsingusõna kohta.
