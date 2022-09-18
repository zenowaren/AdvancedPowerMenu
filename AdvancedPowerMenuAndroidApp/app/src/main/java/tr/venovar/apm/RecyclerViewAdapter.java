package tr.venovar.apm;

import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageButton;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.constraintlayout.widget.ConstraintLayout;
import androidx.recyclerview.widget.RecyclerView;

import com.google.android.material.dialog.MaterialAlertDialogBuilder;

import java.io.IOException;
import java.io.OutputStream;
import java.io.PrintWriter;
import java.net.InetSocketAddress;
import java.net.Socket;
import java.net.SocketAddress;
import java.util.ArrayList;

public class RecyclerViewAdapter extends RecyclerView.Adapter<RecyclerViewAdapter.ViewHolder> {

    public Context _context;
    public ArrayList<Clients> _taskList;

    public RecyclerViewAdapter(Context _context, ArrayList<Clients> _taskList) {
        this._context = _context;
        this._taskList = _taskList;
    }

    @NonNull
    @Override
    public RecyclerViewAdapter.ViewHolder onCreateViewHolder(@NonNull ViewGroup viewGroup, int i) {
        return new RecyclerViewAdapter.ViewHolder(LayoutInflater.from(viewGroup.getContext()).inflate(R.layout.item_client_list, viewGroup, false));
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerViewAdapter.ViewHolder viewHolder, int i) {
        String showResult = _taskList.get(i).getClientIP() + ":" + _taskList.get(i).getClientPort();
        viewHolder.tvClient.setText(_taskList.get(i).getClientName());
        viewHolder.tvUserName.setText(_taskList.get(i).getUserName());
        viewHolder.tvIpAddress.setText(showResult);

        viewHolder.ibShutdown.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                new MaterialAlertDialogBuilder(_context, R.style.CustomMaterialAlertDialog)
                        .setTitle(_context.getString(R.string.attention))
                        .setMessage(_taskList.get(viewHolder.getAdapterPosition()).getClientName() + " " + _context.getString(R.string.shutdown_prompt))
                        .setCancelable(false)
                        .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                sendCommand(_taskList.get(viewHolder.getAdapterPosition()).getClientIP(), _taskList.get(viewHolder.getAdapterPosition()).getClientPort(), "GET / HTTP/1.1 action_is=Shutdown");
                                _taskList.remove(viewHolder.getAdapterPosition());
                                notifyItemRemoved(viewHolder.getAdapterPosition());
                            }
                        })
                        .setNegativeButton(R.string.cancel, new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                dialog.cancel();
                            }
                        }).show();
            }
        });

        viewHolder.ibRestart.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                new MaterialAlertDialogBuilder(_context, R.style.CustomMaterialAlertDialog)
                        .setTitle(_context.getString(R.string.attention))
                        .setMessage(_taskList.get(viewHolder.getAdapterPosition()).getClientName() + " " + _context.getString(R.string.restart_prompt))
                        .setCancelable(false)
                        .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                sendCommand(_taskList.get(viewHolder.getAdapterPosition()).getClientIP(), _taskList.get(viewHolder.getAdapterPosition()).getClientPort(), "GET / HTTP/1.1 action_is=Restart");
                                _taskList.remove(viewHolder.getAdapterPosition());
                                notifyItemRemoved(viewHolder.getAdapterPosition());
                            }
                        })
                        .setNegativeButton(R.string.cancel, new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                dialog.cancel();
                            }
                        }).show();
            }
        });

        viewHolder.ibLock.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                new MaterialAlertDialogBuilder(_context, R.style.CustomMaterialAlertDialog)
                        .setTitle(_context.getString(R.string.attention))
                        .setMessage(_taskList.get(viewHolder.getAdapterPosition()).getClientName() + " " + _context.getString(R.string.lock_prompt))
                        .setCancelable(false)
                        .setPositiveButton(R.string.ok, new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                 sendCommand(_taskList.get(viewHolder.getAdapterPosition()).getClientIP(),_taskList.get(viewHolder.getAdapterPosition()).getClientPort(), "GET / HTTP/1.1 action_is=Lock");
                            }
                        })
                        .setNegativeButton(R.string.cancel, new DialogInterface.OnClickListener() {
                            @Override
                            public void onClick(DialogInterface dialog, int which) {
                                dialog.cancel();
                            }
                        }).show();
            }
        });

        viewHolder.ibScreenshot.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                Intent intent = new Intent(_context, ViewActivity.class);
                intent.putExtra("clientIP", _taskList.get(viewHolder.getAdapterPosition()).getClientIP());
                intent.putExtra("clientPORT", _taskList.get(viewHolder.getAdapterPosition()).getClientPort());
                _context.startActivity(intent);
            }
        });

        viewHolder.bind(_taskList.get(i));
        viewHolder.ibAction.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                boolean expanded = _taskList.get(viewHolder.getAdapterPosition()).isExpanded();
                _taskList.get(viewHolder.getAdapterPosition()).setExpanded(!expanded);
                notifyItemChanged(viewHolder.getAdapterPosition());
            }
        });

        viewHolder.consMain.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                boolean expanded = _taskList.get(viewHolder.getAdapterPosition()).isExpanded();
                _taskList.get(viewHolder.getAdapterPosition()).setExpanded(!expanded);
                notifyItemChanged(viewHolder.getAdapterPosition());
                for(int j = 0; j< _taskList.size(); j++){
                    if(_taskList.get(j).isExpanded() && j !=viewHolder.getAdapterPosition()){
                        _taskList.get(j).setExpanded(false);
                        notifyItemChanged(j);
                    }
                }

            }
        });
    }

    public void sendCommand(String ipAddress, int PORT, String command){
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

    @Override
    public int getItemCount() {
        return _taskList.size();
    }

    public static class ViewHolder extends RecyclerView.ViewHolder{

        final private TextView tvClient, tvUserName, tvIpAddress;
        final private ImageView ibShutdown, ibRestart, ibLock, ibScreenshot;
        final private ImageButton ibAction;
        final private ConstraintLayout consMain, consBottom;

        private ViewHolder(View itemView){
            super(itemView);
            tvClient = itemView.findViewById(R.id.tvClient);
            tvUserName = itemView.findViewById(R.id.tvUserName);
            tvIpAddress = itemView.findViewById(R.id.tvIpAddress);
            ibShutdown = itemView.findViewById(R.id.ibShutdown);
            ibRestart = itemView.findViewById(R.id.ibRestart);
            ibLock = itemView.findViewById(R.id.ibLock);
            ibScreenshot = itemView.findViewById(R.id.ibScreenshot);
            ibAction = itemView.findViewById(R.id.ibAction);
            consMain = itemView.findViewById(R.id.consMain);
            consBottom = itemView.findViewById(R.id.consBottom);

        }

        private void bind(Clients clients) {
            boolean expanded = clients.isExpanded();
            consBottom.setVisibility(expanded ? View.VISIBLE : View.GONE);
            ibAction.setImageResource(expanded ? R.drawable.ic_up : R.drawable.ic_down);
        }
    }
}