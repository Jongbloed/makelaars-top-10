** Werkwijze en toelichting

Ik heb de API verkend met Postman, om te zien wat de structuur is van de responses, welke props ik daarvan nodig heb et cetera.
En hoe de paging werkt. Als je namelijk alle makelaars wilt ordenen op wie er de meeste objecten heeft, en je hebt alleen de endpoint die objecten teruggeeft, moet je alle objecten ophalen.

Ik heb natuurlijk geprobeerd de pagesize in te stellen op een groot getal, maar het wordt gecapped op 25. 
Ook al krijg je maximaal 25 resultaten terug, het "AantalPaginas" veld wordt nog wel kleiner met een pagesize van > 25.
Dat klopt dan dus niet meer. Zou je een bug kunnen noemen. Ik vermoed dat de api in zijn originele versie niet beperkt is tot 25, en dat dat in deze versie wel is doorgevoerd op de ene plek, maar niet op de plek waar de paginaberekening wordt gemaakt. 

Ik heb toen funda.nl verkend, om te zien of die directe mogelijkheden heeft tot het ophalen van makelaars en diens aantal objecten.
Dat kan: /makelaars/{makelaarid} werkt en geeft een pagina terug die in principe alle informatie bevat over hoeveel objecten de makelaar heeft in welke plaats (te zien in de dropdown).
Het /makelaars/ patroon heb ik geprobeerd in de API te vinden, maar tevergeefs. Natuurlijk zou je de html kunnen scrapen voor direct resultaat, maar dat leek me binnen deze scope te lang duren, en daarnaast leek het me beter om in eerste instantie gebruik te maken van wat er gegeven is.

Dus: we hebben een x aantal API-requests die gedaan moeten worden, voor dat we alle gegevens hebben die nodig zijn voor een accurate top 10.
Dat zijn de parameters. 
Natuurlijk zou je de gebruiker kunnen laten wachten tot ze allemaal klaar zijn, but _where's the fun in that_?

Dus ik dacht: hoe kunnen we hier iets cools mee doen.
En wat mij zelf wel cool leek, is als je de top 10-tabel live zou kunnen zien ontwikkelen, en met elke nieuwe response die binnenkomt, verfijnd worden.
Dan wordt het een soort scorebord, dat na elke nieuwe lading data weer wordt geupdate, waarbij sommige makelaars naar boven gaan in de ranglijst, en andere naar beneden.

En wat ik dan zelf wel cool vind, is als al die requests asynchroon uitgevoerd worden, zodat de volgorde waarop de pagina's terugkomen niet deterministisch is. Dus: soms komen de requests voor pagina 3 en 5 eerder binnen dan die van 1 en 2, en soms niet. Dat zorgt er voor dat de ontwikkeling van de scoretabel elke keer anders kan verlopen, hoewel er wel uiteindelijk dezelfde score uit komt.

De constraints zijn verder dat als je meer dan 100 requests doet binnen een minuut, krijg je status 401 terug. Dat weet ik, want natuurlijk heb ik eerst geprobeerd om alles tegelijk af te vuren.
Er is dus een stukje code nodig dat zorgt dat er niet meer dan 100 requests per minuut worden gedaan. 
De simpelste oplossing is natuurlijk om na elke request 600ms te delayen. _but where's the fun in that_?
Dan zou het niet-deterministische aspect verloren gaan, want de API reageert best snel.

Daarom vond ik het leuker om de benodigde pagina's in batches van 100 te verdelen, die zo snel mogelijk af te vuren, en daarna gewoon de rest van de minuut af te wachten.
Dat heeft ook als voordeel dat je zo snel mogelijk resultaat krijgt. In de zoekopdracht met /amsterdam/tuin/ bijvoorbeeld, krijg je minder dan 100 pagina's terug, dus hoeft de gebruiker helemaal niet te wachten.


Ik merkte dat het updaten van de tabel razendsnel gaat, en ik vond het juist wel leuk om de ontwikkeling een beetje te volgen. Dus ik heb een optie ingebouwd om de stappen vertraagd weer te geven.
Het leukste is dan dat de data nog wel steeds zo snel mogelijk geladen wordt. En dat je dan ziet dat de data al 85% binnen is, terwijl je nog naar de ontwikkeling van de top 10 zit te kijken op een tempo dat je kunt volgen.

Ik zou dan een aparte thread kunnen maken die alle requests doet en afwacht, en het resultaat ervan zodra het binnen is op een queue zet. 
Ik zou dan een andere thread kunnen maken, verantwoordelijk voor het weergeven op een tempo dat de gebruiker kan volgen, die van diezelfde queue blokjes data opeet.
Dus ik kon de blocking queue uit de kast trekken.

Ik heb nog gecontroleerd of je wel naar de console mag schrijven vanaf een andere thread, maar dat mag gelukkig om dat die streams gesynchroniseerd worden.

Ik liep er nog tegenaan dat ik inderdaad probeerde om de voortgang van de api data naar het scherm te schrijven vanaf die thread, terwijl de tabel geupdate werd vanuit een andere. Dat werkt, behalve dat het de cursorpositie in de war stuurt.
Ik heb overwogen om de voortgang ("pagina x van y is binnen") aan de queue messages te plakken, maar dan zou het niet meer kloppen in het geval dat de gebruiker er voor kiest om het in zijn eigen tempo weer te geven. De data is dan eigenlijk al veel verder binnen, en je kijkt naar het verleden.
En het leek me juist enig om de data voortgang wel in real time te zien.
Dus heb ik ervoor gekozen om een apart gedeeld object te maken, dat geupdate wordt door de data thread, en door de output thread aangeroepen wordt om zich naar het scherm te schrijven. Op die manier zie je wel de echte voortgang, en wordt niet de schermuitvoer in de war gestuurd.