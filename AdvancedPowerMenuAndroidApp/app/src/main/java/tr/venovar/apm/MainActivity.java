package tr.venovar.apm;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.constraintlayout.widget.ConstraintLayout;
import androidx.core.content.ContextCompat;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;
import androidx.swiperefreshlayout.widget.SwipeRefreshLayout;
import androidx.preference.PreferenceManager;

import android.annotation.SuppressLint;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.os.Handler;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.Menu;
import android.view.MenuInflater;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.Toast;

import com.google.android.material.dialog.MaterialAlertDialogBuilder;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.net.SocketAddress;
import java.util.ArrayList;

public class MainActivity extends AppCompatActivity {

    public AlertDialog _alertDialog;
    public ConstraintLayout consEmpty;
    public SwipeRefreshLayout swipeRefreshLayout;
    public RecyclerView clientsRecyclerView;
    public RecyclerViewAdapter clientsRecyclerViewAdapter;
    public LinearLayoutManager clientsRecyclerViewLayoutManager;
    public View _customAlertDialogView;
    public ViewGroup _nullParent = null;
    public ArrayList<String> ipList = new ArrayList<>();
    public ArrayList<Clients> resultList = new ArrayList<>();

    public static final String MESSAGE = "GET /?inquiry_is=Way HTTP/1.1";
    public static int PORT;
    public static final int TIMEOUT = 500;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        SharedPreferences preferences = PreferenceManager.getDefaultSharedPreferences(MainActivity.this);
        PORT = preferences.getInt("Port", 5001);

        consEmpty = findViewById(R.id.consEmpty);
        clientsRecyclerView = findViewById(R.id.clientsRecyclerView);
        swipeRefreshLayout = findViewById(R.id.srlRefresh);
        swipeRefreshLayout.setRefreshing(false);
        ipList.clear();

        setTitle(getResources().getString(R.string.apm));

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

                        InputStream inputStream = socket.getInputStream();
                        byte[] buffer = new byte[512];
                        int read;

                        StringBuilder stringBuilder = new StringBuilder();
                        while((read = inputStream.read(buffer)) != -1) {
                            String output = new String(buffer, 0, read);
                            stringBuilder.append(output);
                        };

                        Log.d("TAG", "Message Received: " + stringBuilder);

                        if(stringBuilder.length() > 0){

                            String data = stringBuilder.toString();
                            String[] output = data.split("\\|");

                            resultList.add(new Clients(output[0], output[1], ipList.get(i), PORT, false));
                        }

                        inputStream.close();
                        printWriter.close();
                        outputStream.close();
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

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        MenuInflater menuInflater = getMenuInflater();
        menuInflater.inflate(R.menu.main_menu, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(@NonNull MenuItem item) {
        if(item.getItemId() == android.R.id.home){
            Intent _homeIntent = new Intent(MainActivity.this, MainActivity.class);
            _homeIntent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
            startActivity(_homeIntent);
        }else if(item.getItemId() == R.id.action_set_port){
            MaterialAlertDialogBuilder materialAlertDialogBuilderTheme = new MaterialAlertDialogBuilder(MainActivity.this, R.style.CustomMaterialAlertDialog);
            _customAlertDialogView = LayoutInflater.from(MainActivity.this).inflate(R.layout.custom_new_task_dialog, _nullParent, false);
            EditText _edtPort = _customAlertDialogView.findViewById(R.id.edtPort);

            _edtPort.setText(String.valueOf(PORT));
            materialAlertDialogBuilderTheme
                    .setView(_customAlertDialogView)
                    .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
                        @Override
                        public void onClick(DialogInterface dialogInterface, int i) {
                            String tvTaskName = _edtPort.getText().toString();
                            SharedPreferences preferences = PreferenceManager.getDefaultSharedPreferences(MainActivity.this);
                            SharedPreferences.Editor editor = preferences.edit();
                            if(tvTaskName.length() > 0){
                                editor.putInt("Port",Integer.parseInt(tvTaskName));
                                editor.apply();
                            }else{
                                Toast.makeText(MainActivity.this, "Invalid Port", Toast.LENGTH_SHORT).show();
                            }
                        }
                    })
                    .setNegativeButton(R.string.cancel, new DialogInterface.OnClickListener() {
                        @Override
                        public void onClick(DialogInterface dialog, int which) {
                            dialog.cancel();
                        }
                    });
            _alertDialog = materialAlertDialogBuilderTheme.show();
        }
        return super.onOptionsItemSelected(item);
    }
}