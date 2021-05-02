using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client
{
    public partial class Form1 : Form
    {
        // bools to check the conditions in some parts of the code
        bool terminating = false;
        bool connected = false;
        List<string> my_friends = new List<string>();
        List<string> my_requests = new List<string>();
        Socket clientSocket; // client socket
        string user_code;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            // form closing part to handle with crashes
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
            // GUI initialization
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;
            accept_button.Enabled = false;
            reject_button.Enabled = false;
            checkBox3.Enabled = false;
            textBox1.Enabled = false;
            disconnect.Enabled = false;
        }


        private void button_connect_Click(object sender, EventArgs e)   //the button for connecting to the server
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);     // new slient socket is created
            string IP = textBox_ip.Text; // ip is taken by user as port num after this

            int portNum;
            if (IP != "")
            {
                if (Int32.TryParse(textBox_port.Text, out portNum))
                {
                    try
                    {
                        clientSocket.Connect(IP, portNum);  // connection to server
                        // appropriate button and check box enabling
                        button_connect.Enabled = false;
                        textBox_message.Enabled = true;
                        button_send.Enabled = true;
                        checkBox2.Enabled = true;
                        accept_button.Enabled = true;
                        checkBox3.Enabled = true;
                        reject_button.Enabled = true;
                        checkBox1.Enabled = true;
                        disconnect.Enabled = true;
                        textBox1.Enabled = true;
                        connected = true;
                        logs.AppendText("Connected to the server!\n");

                        Thread receiveThread = new Thread(Receive); // thread sent to the receive function
                        receiveThread.Start();

                    }
                    catch
                    {
                        logs.AppendText("Could not connect to the server!\n");  // connection did not start
                    }
                }
                else
                {
                    logs.AppendText("Check the port\n");    // problem in port number
                }
            }
            else
            {
                logs.AppendText("Check the IP\n");
            }
            string username = usernametext.Text;    // username taken from user in GUI

            if (username != "" && username.Length <= 1024) // basic username check
            {
                if (IP != "") // basic IP check
                {
                    try
                    {

                        Byte[] buffer = new Byte[1024];
                        buffer = Encoding.Default.GetBytes(username);
                        clientSocket.Send(buffer);
                        user_code = username;
                    }
                    catch
                    {
                        logs.AppendText("Caught");
                    }
                }
            }

        }

        private void Receive()
        {
            while (connected) // do the following actions when connected
            {
                try
                {
                    // create a buffer array that is received from server
                    Byte[] buffer = new Byte[1024];
                    int received_bytes = clientSocket.Receive(buffer);

                    if (received_bytes == 0)
                    {
                        throw new ObjectDisposedException("NA");    // exception handling
                    }
                    // message from server -> convert to string
                    string incomingMessage = Encoding.Default.GetString(buffer);
                    incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                    int start = incomingMessage.IndexOf('+');

                    // if any request has arrived
                    // do the following
                    if (incomingMessage.Substring(0, start + 1) == "RQ+")
                    {
                        string message = incomingMessage.Substring(start + 1);
                        my_requests.Add(message);
                        request_richtextbox.Text = "";
                        foreach (string s in my_requests)
                        {
                            request_richtextbox.AppendText(s + "\n");
                        }
                    }
                    // if any positive request answer has arrived
                    // do the following
                    else if (incomingMessage.Substring(0, start + 1) == "OK+")
                    {
                        // split the string from the protocol
                        string new_friend = incomingMessage.Substring(start + 1);

                        // keep splitting if more exist in the string
                        while (new_friend.Contains("OK+"))
                        {
                            string m = new_friend.Substring(0, new_friend.IndexOf("OK"));
                            if (!my_friends.Contains(m))
                            {
                                // if do not have this particular friend in my list add it.
                                my_friends.Add(m);
                            }
                            new_friend = new_friend.Substring(new_friend.IndexOf("OK") + 3);
                        }
                        if (!my_friends.Contains(new_friend))
                        {
                            my_friends.Add(new_friend);
                        }
                        // clear the friends window and re-display all
                        richTextBox1.Text = "";
                        foreach (string s in my_friends)
                        {
                            richTextBox1.AppendText(s + "\n");
                        }
                        if (my_requests.Contains(new_friend))
                        {
                            my_requests.Remove(new_friend);
                            // clear the requests window and re-display all
                            request_richtextbox.Text = "";
                            foreach (string s in my_requests)
                            {
                                request_richtextbox.AppendText(s + "\n");
                            }
                        }
                    }
                    // if any requests has arrived when offline
                    // do the following
                    else if (incomingMessage.Substring(0, start + 1) == "NP+")
                    {
                        // split the string from the protocol
                        string temp = incomingMessage.Substring(incomingMessage.IndexOf("NP") + 3);
                        // keep splitting if more exist in the string
                        while (temp.Contains("NP+"))
                        {
                            string m = temp.Substring(0, temp.IndexOf("NP"));
                            if (!my_requests.Contains(m))
                            {
                                // if do not have this particular request in my list add it.
                                my_requests.Add(m);
                            }

                            temp = temp.Substring(temp.IndexOf("NP") + 3);
                        }
                        my_requests.Add(temp);
                        // clear the requests window and re-display all
                        request_richtextbox.Text = "";
                        foreach (string s in my_requests)
                        {
                            request_richtextbox.AppendText(s + "\n");
                        }
                    }
                    // if any positive request answer has arrived when offline
                    // do the following
                    else if (incomingMessage.Substring(0, start + 1) == "NF+")  // work on it
                    {
                        // split the string from the protocol
                        string temp1 = incomingMessage.Substring(incomingMessage.IndexOf("NF") + 3);
                        // keep splitting if more exist in the string
                        while (temp1.Contains("NF+"))
                        {

                            string m = temp1.Substring(0, temp1.IndexOf("NF"));
                            if (!my_friends.Contains(m))
                            {
                                // if do not have this particular friend in my list add it.
                                my_friends.Add(m);
                            }
                            temp1 = temp1.Substring(temp1.IndexOf("NF") + 3);
                        }
                        if (!my_friends.Contains(temp1))
                        {
                            my_friends.Add(temp1);
                        }
                        // clear the requests window and re-display all
                        richTextBox1.Text = "";
                        foreach (string s in my_friends)
                        {
                            richTextBox1.AppendText(s + "\n");
                        }
                    }
                    // if any message has arrived when offline
                    // do the following
                    else if (incomingMessage.Substring(0, start + 1) == "MP+")
                    {
                        // split the string from the protocol
                        string temp1 = incomingMessage.Substring(incomingMessage.IndexOf("MP") + 3);
                        // keep splitting if more exist in the string
                        while (temp1.Contains("MP+"))
                        {
                            // display the message
                            logs.AppendText(temp1 + "\n");
                            temp1 = temp1.Substring(temp1.IndexOf("MP") + 3);
                        }
                        
                        logs.AppendText(temp1 + "\n");

                    }
                    // if tried to remove a friend that is not in your list
                    // do the following
                    else if (incomingMessage.Substring(0, start + 1) == "UN+")
                    {
                        string message = incomingMessage.Substring(start + 1);
                        string mes = "You can't remove " + message + " who doesn't exist in your friends list \n";
                        logs.AppendText(mes + "\n");
                    }
                    // if any removing procedure has occured when offline
                    // do the following
                    else if (incomingMessage.Substring(0, start + 1) == "UF+")
                    {
                        string message = incomingMessage.Substring(start + 1);
                        my_friends.Remove(message);
                        string mes = usernametext.Text + ", you are no longer friends with " + message;
                        logs.AppendText(mes + "\n");
                        richTextBox1.Text = "";
                        foreach (string s in my_friends)
                        {
                            richTextBox1.AppendText(s + "\n");
                        }
                    }
                    // if any rejection has occured when offline
                    // do the following
                    else if (incomingMessage.Substring(0, start + 1) == "MS+")
                    {
                        string message = incomingMessage.Substring(start + 1);
                        string mes = usernametext.Text + ", your friend request is rejected by " + message;
                        logs.AppendText(mes + "\n");
                    }
                    else
                    {
                        logs.AppendText(incomingMessage + "\n"); // display the message
                    }
                }
                catch
                {
                    if (!connected)
                    {
                        logs.AppendText("You are disconnected \n");     // appropriate message is displayed when there is no connection any more
                        button_connect.Enabled = true;
                        disconnect.Enabled = false;
                        textBox_message.Enabled = false;
                        checkBox1.Enabled = false;
                        textBox1.Enabled = false;
                        checkBox3.Enabled = false;
                        checkBox2.Enabled = false;
                        button_send.Enabled = false;
                    }

                    if (!terminating && connected)  // appropriate message is displayed when there is no connection by server sides
                    {
                        logs.AppendText("The server has disconnected \n");
                        button_connect.Enabled = true;
                        disconnect.Enabled = false;
                        textBox_message.Enabled = false;
                        checkBox1.Enabled = false;
                        checkBox3.Enabled = false;
                        textBox1.Enabled = false;
                        checkBox2.Enabled = false;
                        button_send.Enabled = false;
                    }
                    clientSocket.Close();
                    connected = false;
                }
            }

        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)  // crashing handler
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_send_Click(object sender, EventArgs e)  // send data to server
        {
            string message = textBox_message.Text;  // data is taken from user
            string friends = textBox_message.Text;

            if (message != "" && message.Length <= 1024)
            {
                if (checkBox1.Checked == true)
                {
                    if (friends != usernametext.Text)
                    {
                        message = "FR+" + textBox_message.Text;
                        Byte[] buffer = new Byte[1024];
                        buffer = Encoding.Default.GetBytes(message);
                        clientSocket.Send(buffer);
                        checkBox1.Checked = false;
                        textBox_message.Text = "";
                    }
                    else
                    {
                        logs.AppendText("Can't be friend with yourself");
                    }
                }
                else if (checkBox2.Checked == true)
                {
                    if (friends != usernametext.Text)
                    {
                        // friend to remove
                        message = "RM+" + textBox_message.Text;
                        Byte[] buffer = new Byte[1024];
                        buffer = Encoding.Default.GetBytes(message);
                        clientSocket.Send(buffer);
                        checkBox2.Checked = false;
                        textBox_message.Text = "";
                    }
                    else
                    {
                        logs.AppendText("Can't remove yourself");
                    }
                }
                else if (checkBox3.Checked == true)
                {
                    // sending message only to the friends
                    message = "MG+" + textBox_message.Text;
                    Byte[] buffer = new Byte[1024];
                    buffer = Encoding.Default.GetBytes(message);
                    clientSocket.Send(buffer);
                    checkBox3.Checked = false;
                    textBox_message.Text = "";
                }
                else
                {
                    // sending message to all
                    message = "MS+" + textBox_message.Text;
                    Byte[] buffer = new Byte[1024];
                    buffer = Encoding.Default.GetBytes(message);
                    clientSocket.Send(buffer);
                    textBox_message.Text = "";
                }
            }
            checkBox2.Enabled = true;
            checkBox1.Enabled = true;

        }

        private void disconnect_Click(object sender, EventArgs e)   // in case of user want to disconnect from the server
        {
            string message = "disconnected"; // send the message "disconnected" and then leave
            Byte[] buffer = new Byte[1024];
            buffer = Encoding.Default.GetBytes(message);
            clientSocket.Send(buffer);
            disconnect.Enabled = false;
            accept_button.Enabled = false;
            reject_button.Enabled = false;
            checkBox2.Enabled = false;
            checkBox3.Enabled = false;
            connected = false;
            clientSocket.Disconnect(false);
            clientSocket.Close();
            richTextBox1.Text = "";
            request_richtextbox.Text = "";
            my_requests.Clear();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            // friend request operation
            if (connected)
            {
                checkBox1.Enabled = true;
                if (checkBox1.Checked == true)
                {
                    checkBox2.Enabled = false;
                    checkBox3.Enabled = false;
                    logs.AppendText("Please write a username to send a friend request and click on send \n");
                }
                else
                {
                    checkBox2.Enabled = true;
                    checkBox3.Enabled = true;
                }
            }
            else
            {
                checkBox1.Enabled = false;
            }
        }

        private void request_richtextbox_TextChanged(object sender, EventArgs e)
        {

        }

        private void accept_button_Click(object sender, EventArgs e)
        {
            string message = textBox1.Text;  // data is taken from user
            string friend = textBox1.Text;
            if (message != "" && message.Length <= 1024)
            {
                if (my_requests.Contains(friend))
                {
                    // acceptance protocol
                    message = "AC+" + message;
                    Byte[] buffer = new Byte[1024];
                    buffer = Encoding.Default.GetBytes(message);
                    clientSocket.Send(buffer);
                }
                // if my requests are empty
                if (my_requests.Count() == 0)
                {
                    logs.AppendText("There is no request from any user");
                }
                else if (my_requests.Contains(friend))
                {
                    logs.AppendText("");
                }
                // if there is no particular request
                else
                {
                    logs.AppendText("There is no request from the user");
                }

            }
            textBox1.Text = "";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void reject_button_Click(object sender, EventArgs e)
        {
            string message = textBox1.Text;  // data is taken from user
            string friend = textBox1.Text;
            if (message != "" && message.Length <= 1024)
            {
                // rejection protocol
                message = "RJ+" + message;
                Byte[] buffer = new Byte[1024];
                buffer = Encoding.Default.GetBytes(message);
                clientSocket.Send(buffer);
                textBox1.Text = "";
                // if my requests are empty
                if (my_requests.Count() == 0)
                {
                    logs.AppendText("There is no request from any user");
                }
                textBox1.Text = "";
                if (my_requests.Count() != 0)
                {
                    if (my_requests.Contains(friend))
                    {
                        my_requests.Remove(friend);
                        request_richtextbox.Text = "";
                        foreach (string s in my_requests)
                        {
                            request_richtextbox.AppendText(s + "\n");
                        }
                    }
                }
            }
            textBox1.Text = "";
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox_message_TextChanged(object sender, EventArgs e)
        {

        }

        private void logs_TextChanged(object sender, EventArgs e)
        {

        }

        private void usernametext_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            // friend removal operation
            if (connected)
            {
                checkBox2.Enabled = true;
                if (checkBox2.Checked == true)
                {
                    checkBox1.Enabled = false;
                    checkBox3.Enabled = false;
                    logs.AppendText("Please write a username to remove a friend and click on send \n");
                }
                else
                {
                    checkBox1.Enabled = true;
                    checkBox3.Enabled = true;
                }
            }
            else
            {
                checkBox2.Enabled = false;
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (connected)
            {
                checkBox3.Enabled = true;
                if (checkBox3.Checked == true)
                {
                    checkBox1.Enabled = false;
                    checkBox2.Enabled = false;
                    logs.AppendText("Message will be delivered only to your friends \n");
                }
                else
                {
                    checkBox1.Enabled = true;
                    checkBox2.Enabled = true;
                }
            }
            else
            {
                checkBox3.Enabled = false;
            }
        }
    }
}
