function getIndexOfCity(name) {

    for (var i = 0; i < cities.length; i++) {
        if (cities[i].Name == name) {
            return i;
        }
    }
    return -1;
}
