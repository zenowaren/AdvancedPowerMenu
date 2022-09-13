package tr.venovar.powermenu;

public class Clients {

    public String clientName;
    public String clientIP;
    public int clientPort;

    public Clients(String clientName, String clientIP, int clientPort) {
        this.clientName = clientName;
        this.clientIP = clientIP;
        this.clientPort = clientPort;
    }

    public String getClientName() {
        return clientName;
    }

    public void setClientName(String clientName) {
        this.clientName = clientName;
    }

    public String getClientIP() {
        return clientIP;
    }

    public void setClientIP(String clientIP) {
        this.clientIP = clientIP;
    }

    public int getClientPort() {
        return clientPort;
    }

    public void setClientPort(int clientPort) {
        this.clientPort = clientPort;
    }
}
