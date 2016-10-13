// This file is required by the index.html file and will
// be executed in the renderer process for that window.
// All of the Node.js APIs are available in this process.
const $ = require('jquery');


webSocket = connect();

function onopen() {
    $("#messageBoard").append("<p>Connected<p>");
}

function onmessage(evt) {
    $("#messageBoard").append("<p>" + evt.data + "<p>");
}

function onerror(evt) {
    alert(evt.message);
}

function onclose() {
    $("#messageBoard").append("<p>Disconnected<p>");
}

$("#btnSend").click(() => {
    if (webSocket.readyState == WebSocket.OPEN) {
        webSocket.send($("#textInput").val());
    }
    else {
        $("#messageBoard").append("<p>Connection is closed<p>");
    }
});

$('#btnClear').click(() => {
    $("#messageBoard").empty();
});

$('#btnReconnect').click(() => {
    webSocket = connect();
});

function connect() {
    ws = new WebSocket("ws://wscagents.westus.cloudapp.azure.com");
    ws.onopen = onopen;
    ws.onmessage = onmessage;
    ws.onerror = onerror;
    ws.onclose = onclose;

    return ws;
}