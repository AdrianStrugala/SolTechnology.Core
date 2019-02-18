/// <reference path="../../lib/googleMaps/googleMaps.d.ts" />

function displayRouteLabel(map, latitude, longtitude, text) {

    var iconSize = new google.maps.Size(text.length * 0.6, 4, "em", "em");
    var defaultFontSize = getDefaultFontSize();


    var marker = new google.maps.Marker(({
        position: {
            lat: latitude,
            lng: longtitude
        },
        map: map,
        opacity: 0.8,
        label: {
            text: text.toString(),
            fontWeight: "bold"
        },
        icon: {
            scaledSize: iconSize,
            labelOrigin: new google.maps.Point(iconSize.width * (defaultFontSize * 0.8) / 2, iconSize.height * (defaultFontSize * 0.8)/2),
            url: "../images/labelFrame.jpg"
        }
    }) as any);

    return marker;
}

function getDefaultFontSize(): number {
    var pa = document.body;
    var who = document.createElement('div');

    who.style.cssText = 'display:inline-block; padding:0; line-height:1; position:absolute; visibility:hidden; font-size:1em';

    who.appendChild(document.createTextNode('M'));
    pa.appendChild(who);
    var fs = who.offsetWidth;
    pa.removeChild(who);
    return fs;
}
