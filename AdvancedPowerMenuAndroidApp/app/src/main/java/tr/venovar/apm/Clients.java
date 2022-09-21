package tr.venovar.apm;

public class Clients {

    public String clientName;
    public String userName;
    public String clientIP;
    public int clientPort;
    private boolean expanded;

    public Clients(String clientName, String userName, String clientIP, int clientPort, boolean expanded) {
        this.clientName = clientName;
        this.userName = userName;
        this.clientIP = clientIP;
        this.clientPort = clientPort;
        this.expanded = expanded;
    }

    public String getClientName() {
        return clientName;
    }

    public void setClientName(String clientName) {
        this.clientName = clientName;
    }

    public String getUserName() {
        return userName;
    }

    public void setUserName(String userName) {
        this.userName = userName;
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

    public boolean isExpanded() {
        return expanded;
    }

    public void setExpanded(boolean expanded) {
        this.expanded = expanded;
    }
}
