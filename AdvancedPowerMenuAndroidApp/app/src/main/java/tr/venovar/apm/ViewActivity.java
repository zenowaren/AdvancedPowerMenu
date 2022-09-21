package tr.venovar.apm;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.widget.AppCompatImageView;

import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.os.Bundle;
import android.util.Log;
import android.view.Menu;
import android.view.MenuInflater;
import android.view.MenuItem;

import java.io.ByteArrayOutputStream;
import java.io.DataInputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.net.SocketAddress;
import java.nio.ByteBuffer;

public class ViewActivity extends AppCompatActivity {

    public String clientIP;
    public int clientPORT;
    public AppCompatImageView aCivScreenshot;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_view);

        if(getSupportActionBar() != null){
            getSupportActionBar().setDisplayHomeAsUpEnabled(true);
            getSupportActionBar().setDisplayShowHomeEnabled(true);
        }

        setTitle(getResources().getString(R.string.screenshot_view));

        aCivScreenshot = findViewById(R.id.aCivScreenshot);

        Bundle extras = getIntent().getExtras();
        if(extras != null) {
            clientIP = extras.getString("clientIP");
            clientPORT  = extras.getInt("clientPORT");
         }

        sendCommand(clientIP, clientPORT);
    }

    public void sendCommand(String ipAddress, int PORT){
        new Thread(){
            @Override
            public void run() {
                try {
                    SocketAddress socketAddress = new InetSocketAddress(ipAddress, PORT);
                    Socket socket = new Socket();
                    socket.connect(socketAddress, 500);

                    OutputStream outputStream = socket.getOutputStream();

                    PrintWriter printWriter = new PrintWriter(outputStream);
                    printWriter.print("GET /?action_is=MobileApp_Screenshot HTTP/1.1");
                    printWriter.flush();

                    Log.i("TAG", "Command Sent");

                    DataInputStream dataInputStream = new DataInputStream(socket.getInputStream());
                    ByteArrayOutputStream byteArrayOutputStream = new ByteArrayOutputStream();

                    byte[] buffer = new byte[512];

                    int bits_read = 0;

                    do
                    {
                        byteArrayOutputStream.write(buffer, 0, bits_read);
                    }
                    while((bits_read=dataInputStream.read(buffer))>=0);

                    byte[] result = byteArrayOutputStream.toByteArray();

                    Log.i("TAG", "Data Size Received: " + ByteBuffer.wrap(buffer).getInt());

                    runOnUiThread(new Runnable() {
                        @Override
                        public void run() {
                                Bitmap bitmap = BitmapFactory.decodeByteArray(result, 0, result.length);
                            if(bitmap != null){
                                bitmap.compress(Bitmap.CompressFormat.PNG, 100, byteArrayOutputStream);
                                aCivScreenshot.setImageBitmap(bitmap);
                            }
                        }
                    });

                    byteArrayOutputStream.close();
                    dataInputStream.close();
                    printWriter.close();
                    socket.close();
                } catch (IOException e) {
                    Log.e("TAG", "sendCommand Exception: " + e);
                }
            }
        }.start();
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        MenuInflater menuInflater = getMenuInflater();
        menuInflater.inflate(R.menu.view_menu, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(@NonNull MenuItem item) {
        if(item.getItemId() == android.R.id.home){
            Intent _homeIntent = new Intent(ViewActivity.this, MainActivity.class);
            _homeIntent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);
            startActivity(_homeIntent);
        }else if(item.getItemId() == R.id.action_refresh_view){
            sendCommand(clientIP, clientPORT);
        }
        return super.onOptionsItemSelected(item);
    }
}