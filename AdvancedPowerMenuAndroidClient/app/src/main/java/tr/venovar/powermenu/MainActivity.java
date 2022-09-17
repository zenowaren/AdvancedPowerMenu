package tr.venovar.powermenu;

import androidx.appcompat.app.AppCompatActivity;
import androidx.constraintlayout.widget.ConstraintLayout;
import androidx.core.content.ContextCompat;
import androidx.recyclerview.widget.GridLayoutManager;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout;

import android.annotation.SuppressLint;
import android.os.Bundle;
import android.os.Handler;
import android.util.Log;
import android.view.View;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.net.SocketAddress;
import java.util.ArrayList;

public class MainActivity extends AppCompatActivity {

    public ConstraintLayout consEmpty;
    public SwipeRefreshLayout swipeRefreshLayout;
    public RecyclerView clientsRecyclerView;
    public RecyclerViewAdapter clientsRecyclerViewAdapter;
    public LinearLayoutManager clientsRecyclerViewLayoutManager;
    public GridLayoutManager clientsGridLayoutManager;
    public ArrayList<String> ipList = new ArrayList<>();
    public ArrayList<Clients> resultList = new ArrayList<>();

    public static final String MESSAGE = "GET / HTTP/1.1 200 OK inquiry_is=Way";
    public static final int PORT = 5001;
    public static final int TIMEOUT = 500;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        consEmpty = findViewById(R.id.consEmpty);
        clientsRecyclerView = findViewById(R.id.clientsRecyclerView);
        swipeRefreshLayout = findViewById(R.id.srlRefresh);
        swipeRefreshLayout.setRefreshing(false);
        ipList.clear();

       for(int i = 0; i < 6; i++){
           ipList.add("192.168.1."+ i);
        }

        swipeRefreshLayout.setColorSchemeResources(R.color.refresh_progress_1, R.color.refresh_progress_2, R.color.refresh_progress_3);
        swipeRefreshLayout.setProgressBackgroundColorSchemeColor(ContextCompat.getColor(MainActivity.this, R.color.colorMainBackground));

        swipeRefreshLayout.setOnRefreshListener(new SwipeRefreshLayout.OnRefreshListener() {
            @Override
            public void onRefresh() {
                swipeRefreshLayout.setRefreshing(true);
                resultList.clear();
                recyclerViewInitialize();
                sendPackage();
                new Handler().postDelayed(new Runnable() {
                    @Override
                    public void run() {
                        swipeRefreshLayout.setRefreshing(false);
                        swipeRefreshLayout.setEnabled(true);
                    }
                }, ((long) TIMEOUT * ipList.size()));
            }
        });
    }

    public static void sendCommand(String ipAddress, String command){
        new Thread(){
            @Override
            public void run() {
                try {
                SocketAddress socketAddress = new InetSocketAddress(ipAddress, PORT);
                Socket socket = new Socket();
                socket.connect(socketAddress, 500);

                OutputStream outputStream = socket.getOutputStream();

                PrintWriter printWriter = new PrintWriter(outputStream);
                printWriter.print(command);
                printWriter.flush();
                printWriter.close();
                outputStream.close();
                socket.close();
                } catch (IOException e) {
                    Log.e("TAG", "sendCommand Exception: " + e);
                }
            }
        }.start();
    }

    public void sendPackage() {
        new Thread() {
            @Override
            public void run() {
                for(int i = 0; i<ipList.size(); i++){
                    try {
                        SocketAddress socketAddress = new InetSocketAddress(ipList.get(i), PORT);
                        Socket socket = new Socket();
                        socket.connect(socketAddress, 500);

                        OutputStream outputStream = socket.getOutputStream();

                        PrintWriter printWriter = new PrintWriter(outputStream);
                        printWriter.print(MESSAGE);
                        printWriter.flush();

                        BufferedReader bufferedReader = new BufferedReader(new InputStreamReader(socket.getInputStream()));

                        String line;
                        StringBuilder stringBuilder = new StringBuilder();

                        while ((line = bufferedReader.readLine()) != null) {
                            stringBuilder.append(line);
                        }

                        Log.d("TAG", "Message Received: " + stringBuilder);

                        if(stringBuilder.length() > 0){
                            resultList.add(new Clients(stringBuilder.toString(), ipList.get(i), PORT, false));
                        }

                        printWriter.close();
                        outputStream.close();
                        bufferedReader.close();
                        socket.close();
                    } catch (IOException e) {
                        Log.e("TAG", "sendPackage Exception: " + e);
                    }

                    runOnUiThread(new Runnable() {
                        @SuppressLint("NotifyDataSetChanged")
                        @Override
                        public void run() {
                            consEmpty.setVisibility(resultList.size() > 0 ? View.GONE : View.VISIBLE);
                            clientsRecyclerView.setVisibility(resultList.size() > 0 ? View.VISIBLE : View.GONE);
                            clientsRecyclerViewAdapter.notifyDataSetChanged();
                        }
                    });

                }
            }
        }.start();
    }

    public void recyclerViewInitialize(){
        consEmpty.setVisibility(resultList.size() > 0 ? View.GONE : View.VISIBLE);
        clientsRecyclerView.setVisibility(resultList.size() > 0 ? View.VISIBLE : View.GONE);

        clientsRecyclerViewAdapter = new RecyclerViewAdapter(MainActivity.this, resultList);
        clientsRecyclerViewLayoutManager = new LinearLayoutManager(MainActivity.this, LinearLayoutManager.VERTICAL, false);
        clientsRecyclerView.setHasFixedSize(true);
        clientsRecyclerView.setLayoutManager(clientsRecyclerViewLayoutManager);
        clientsRecyclerView.setAdapter(clientsRecyclerViewAdapter);
    }
}