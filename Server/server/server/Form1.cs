using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace server
{
    public partial class Form1 : Form
    {

        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  // basic socket
        List<Socket> clientSockets = new List<Socket>();    // list of client sockets
        List<string> usernames = new List<string>();    // list of usernames that are connected
        List<string> offline_users = new List<string>();
        //List<Socket> discSockets = new List<Socket>();
        Dictionary<string, List<string>> pending_Dict = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> friends = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> pending_messages = new Dictionary<string, List<string>>();

        bool terminating = false;
        bool listening = false;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);    // to handle crashing
            InitializeComponent();
            pending_Dict.Clear();
            friends.Clear();
            pending_messages.Clear();
        }

        private bool isUser(string username)    // function to check if the username is in the list that is provided
        {
            System.IO.StreamReader namefile = new System.IO.StreamReader(@"C:\Users\MSI\Desktop\user_db.txt");
            string line;
            while ((line = namefile.ReadLine()) != null)
            {
                if (username == line)
                {
                    return true;
                }
            }
            return false;
        }

        private bool newUser(string username, List<string> usernames)   // function to check if the username is already connected or not
        {
            if (usernames.Count() == 0)
            {
                return true;
            }
            foreach (string u in usernames)
            {
                if (username == u)
                {
                    return false;
                }
            }
            return true;
        }

        private void button_listen_Click(object sender, EventArgs e)    // starting to listen the requests
        {
            int serverPort;

            if (Int32.TryParse(textBox_port.Text, out serverPort))  // portnumber will be taken from a user
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverSocket.Bind(endPoint);    // bind the socket to endpoint
                serverSocket.Listen(3); // listening

                listening = true;
                button_listen.Enabled = false;

                Thread acceptThread = new Thread(Accept); // thread to send for multi-clients
                acceptThread.Start();

                logs.AppendText("Started listening on port: " + serverPort + "\n");

            }
            else
            {
                logs.AppendText("Please check port number \n");
            }
        }

        private void Accept()
        {
            while (listening)
            {
                try
                {
                    Socket newClient = serverSocket.Accept(); // create new client socket
                    clientSockets.Add(newClient); // add new client socket to the list

                    Thread receiveThread = new Thread(Receive); // send thread to the receive
                    receiveThread.Start();

                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }

        }

        private void Receive()
        {
            if (clientSockets.Count() != 0)
            {
                Socket thisClient = clientSockets[clientSockets.Count() - 1]; // current client

                bool connected;

                Byte[] buffer2 = new Byte[1024];
                thisClient.Receive(buffer2); // message taken from the client

                string incomingUsername = Encoding.Default.GetString(buffer2); // conversion to string
                incomingUsername = incomingUsername.Substring(0, incomingUsername.IndexOf("\0"));
                logs.AppendText(incomingUsername);


                if (!isUser(incomingUsername)) // user check
                {
                    clientSockets.Remove(thisClient);
                    thisClient.Close();

                    logs.AppendText(" is wrong username \n");
                    connected = false;
                }
                else if (!newUser(incomingUsername, usernames)) // new user check
                {
                    clientSockets.Remove(thisClient);
                    thisClient.Close();

                    logs.AppendText(" has same username \n");
                    connected = false;
                }

                else
                {
                    logs.AppendText(" is connected.\n");
                    usernames.Add(incomingUsername);
                    connected = true;
                    // sending pending requests to the offline users when they become online
                    if (offline_users.Contains(incomingUsername))
                    {

                        if (pending_Dict.ContainsKey(incomingUsername))
                        {
                            logs.AppendText("Looking for pending requests... \n");
                            for (int i = 0; i < pending_Dict[incomingUsername].Count(); i++)
                            {
                                string off_code = "NP+" + pending_Dict[incomingUsername][i].ToString();
                                byte[] data = Encoding.ASCII.GetBytes(off_code);
                                int conn_user = usernames.IndexOf(incomingUsername);
                                foreach (Socket s in clientSockets)
                                {
                                    if (s == clientSockets[conn_user])
                                    {
                                        s.Send(data);
                                    }
                                }
                            }
                            logs.AppendText("Pending requests are updated... \n");
                        }
                        // thread to wait for a second
                        System.Threading.Thread.Sleep(1000);
                        // sending friends to the offline users when they become online
                        if (friends.ContainsKey(incomingUsername))
                        {
                            logs.AppendText("Looking for new friends... \n");
                            for (int i = 0; i < friends[incomingUsername].Count(); i++)
                            {
                                string off_code = "NF+" + friends[incomingUsername][i].ToString();
                                byte[] data = Encoding.ASCII.GetBytes(off_code);
                                int conn_user = usernames.IndexOf(incomingUsername);
                                foreach (Socket s in clientSockets)
                                {
                                    if (s == clientSockets[conn_user])
                                    {
                                        s.Send(data);
                                    }
                                }
                            }
                            logs.AppendText("Friends are updated... \n");
                        }
                        // thread to wait for a second
                        System.Threading.Thread.Sleep(1000);
                        // if offline user becomes online and has message that waits
                        if (pending_messages.ContainsKey(incomingUsername))
                        {
                            logs.AppendText("Looking for pending messages... \n");
                            for (int i = 0; i < pending_messages[incomingUsername].Count(); i++)
                            {
                                string off_code = "MP+" + pending_messages[incomingUsername][i].ToString();
                                byte[] data = Encoding.ASCII.GetBytes(off_code);
                                int conn_user = usernames.IndexOf(incomingUsername);
                                foreach (Socket s in clientSockets)
                                {
                                    if (s == clientSockets[conn_user])
                                    {
                                        s.Send(data);
                                    }
                                }
                            }
                            logs.AppendText("Pending messages are updated... \n");
                            // after sending all messages clear the list
                            pending_messages[incomingUsername].Clear();
                        }
                        // remove from offline users list
                        offline_users.Remove(incomingUsername);
                    }
                }

                //

                while (connected && !terminating)
                {
                    try
                    {

                        Byte[] buffer = new Byte[1024];
                        thisClient.Receive(buffer);
                        string sent_user = usernames[find_index(thisClient)]; // client that sent the message
                        string incomingMessage = Encoding.Default.GetString(buffer);
                        int start = incomingMessage.IndexOf('+');
                        incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));
                        string total_message = sent_user + ": " + incomingMessage.Substring(start + 1); // complete message


                        if (incomingMessage == "disconnected") // if message is specifically disconnected
                        {
                            logs.AppendText(incomingUsername +" has disconnected \n");
                            connected = false;
                            clientSockets.Remove(thisClient); // remove from clientsockets list
                            usernames.Remove(incomingUsername); // remove from connected user's list
                            thisClient.Close();
                            offline_users.Add(incomingUsername);
                        }
                        else if (incomingMessage.Substring(0, start + 1) == "MS+")
                        {
                            logs.AppendText("Message has arrived... \n");
                            string message = incomingMessage.Substring(start + 1);
                            logs.AppendText(usernames[find_index(thisClient)] + ": " + message + "\n");
                            byte[] bytes = Encoding.ASCII.GetBytes(total_message);

                            foreach (Socket s in clientSockets) // send message everyone but sender
                            {
                                if (s != thisClient)
                                {
                                    s.Send(bytes);
                                }

                            }
                            logs.AppendText("Message has sent... \n");
                        }
                        // only friends protocol
                        else if (incomingMessage.Substring(0, start + 1) == "MG+")
                        {
                            logs.AppendText("Message between friends has arrived... \n");
                            string message = incomingMessage.Substring(start + 1);
                            logs.AppendText(usernames[find_index(thisClient)] + ": " + message + "\n");
                            string incominguser = usernames[find_index(thisClient)];
                            byte[] bytes = Encoding.ASCII.GetBytes(total_message);
                            List<string> pender = new List<string>();

                            if (friends.ContainsKey(incominguser))
                            {
                                foreach (string friend in friends[incominguser])
                                {
                                    logs.AppendText("Send message(s) immediately if " + friend + " is online! \n");
                                    foreach (Socket s in clientSockets)
                                    {
                                        // if the user is online
                                        // send the message directly
                                        
                                        if (online_usercheck(friend))
                                        {
                                            
                                            int index = usernames.IndexOf(friend);

                                            if (s == clientSockets[index])
                                            {
                                                s.Send(bytes);
                                            }
                                            
                                        }
                                    }
                                    // if the friend user is offline
                                    // add the message to the dictionary
                                    if (!online_usercheck(friend))
                                    {
                                        logs.AppendText(friend + " is not online at the moment, adding the message(s) to the pending list \n");
                                        if (pending_messages.ContainsKey(friend))
                                        {
                                            pending_messages[friend].Add(total_message);
                                        }
                                        else
                                        {
                                            pending_messages.Add(friend, pender);
                                            pending_messages[friend].Add(total_message);
                                        }
                                        logs.AppendText("Message(s) are added to the list \n");
                                    }
                                }

                            }

                        }
                        // friend request protocol
                        else if (incomingMessage.Substring(0, start + 1) == "FR+")
                        {
                            bool friend_check = false;
                            bool request_check = false;
                            string requested_person = incomingMessage.Substring(start + 1);

                            logs.AppendText("Friend request has arrived \n");

                            logs.AppendText("Checking the appropriate conditions for friend request... \n");
                            // check if they are already friends
                            if (friends.ContainsKey(requested_person) && friends.ContainsKey(sent_user))
                            {
                                if (friends[requested_person].Contains(sent_user) && friends[sent_user].Contains(requested_person))
                                {
                                    logs.AppendText("Violation for friend protocol has been determined CODE:0001 \n");
                                    string code_ER = "You are already friends! \n";
                                    friend_check = true;

                                    byte[] data = Encoding.ASCII.GetBytes(code_ER);
                                    int index_requested_person = usernames.IndexOf(sent_user);
                                    foreach (Socket s in clientSockets)
                                    {
                                        if (s == clientSockets[index_requested_person])
                                        {
                                            s.Send(data);
                                        }
                                    }
                                }
                            }
                            // check if the friend request has been send before
                            if (friend_check == false)
                            {
                                List<string> pendingList = new List<string>();
                                if (pending_Dict.ContainsKey(requested_person)) // if I haven't got the request before from requested person
                                {
                                    if (pending_Dict[requested_person].Contains(sent_user))
                                    {
                                        logs.AppendText("Violation for friend protocol has been determined CODE:0002 \n");
                                        string code_ER = "Friend Request has beeen sent before! \n";
                                        request_check = true;

                                        byte[] data = Encoding.ASCII.GetBytes(code_ER);
                                        int index_requested_person = usernames.IndexOf(sent_user);
                                        foreach (Socket s in clientSockets) // send message everyone but sender
                                        {
                                            if (s == clientSockets[index_requested_person])
                                            {
                                                s.Send(data);
                                            }
                                        }
                                    }
                                }

                                if (request_check == false)
                                {
                                    //bool online = false;
                                    logs.AppendText("No violation for friend protocol has been determined \n");
                                    logs.AppendText("Checking if user is online... \n");
                                    if (online_usercheck(requested_person)) // to check if person is online
                                    {
                                        logs.AppendText("User is online \n");
                                        logs.AppendText("Adding requests to user's list \n");
                                        string message = "RQ+" + sent_user;
                                        if (pending_Dict.ContainsKey(requested_person))
                                        {
                                            pending_Dict[requested_person].Add(sent_user);
                                            //online = true;
                                        }
                                        else
                                        {
                                            pending_Dict.Add(requested_person, pendingList);
                                            pending_Dict[requested_person].Add(sent_user);
                                            //online = true;
                                        }
                                        byte[] data = Encoding.ASCII.GetBytes(message);
                                        int index_requested_person = usernames.IndexOf(requested_person);
                                        logs.AppendText("Sending request to the user... \n");
                                        foreach (Socket s in clientSockets)
                                        {
                                            if (s == clientSockets[index_requested_person])
                                            {
                                                s.Send(data);
                                            }
                                        }
                                        logs.AppendText("Request has been sent \n");
                                    }
                                    else
                                    {
                                        logs.AppendText("User is not online \n");
                                        logs.AppendText("Adding requests to user's list \n");
                                        // user is not online
                                        if (pending_Dict.ContainsKey(requested_person))
                                        {
                                            // if dictionary contains the person that has request
                                            // just add the a value to the corresponding key
                                            pending_Dict[requested_person].Add(sent_user);
                                        }
                                        else
                                        {
                                            // if dictionary does't contain the person that has request
                                            // create a specific key
                                            // then add the a value to the corresponding key
                                            pending_Dict.Add(requested_person, pendingList);
                                            pending_Dict[requested_person].Add(sent_user);
                                        }
                                        // since user is not online
                                        // add him to the offline users list
                                        if (!offline_users.Contains(requested_person))
                                        {
                                            offline_users.Add(requested_person);
                                        }
                                    }
                                }
                            }
                        }
                        // acceptance protocol
                        else if (incomingMessage.Substring(0, start + 1) == "AC+")
                        {
                            logs.AppendText("User accepting protocol is activated \n");
                            // split the string from the protocol
                            string accepted_user = incomingMessage.Substring(start + 1);

                            List<string> friender = new List<string>();
                            // if already exist in the dictionary
                            // do the following
                            logs.AppendText("Making friend of both sides... \n");
                            if (friends.ContainsKey(sent_user))
                            {
                                friends[sent_user].Add(accepted_user);
                                pending_Dict[sent_user].Remove(accepted_user);
                            }
                            // if the user do not exist in the dictionary
                            // do the following
                            else
                            {
                                friends.Add(sent_user, friender);
                                friends[sent_user].Add(accepted_user);
                                pending_Dict[sent_user].Remove(accepted_user);
                            }
                            List<string> friend_er = new List<string>();
                            // do the same procedure for the other user
                            if (friends.ContainsKey(accepted_user))
                            {
                                friends[accepted_user].Add(sent_user);
                            }
                            else
                            {
                                friends.Add(accepted_user, friend_er);
                                friends[accepted_user].Add(sent_user);
                            }
                            // after friend operation
                            // send the acknowledgement to both of the users
                            // if user that is accepted is online
                            logs.AppendText("Checking if user is online to acknowledge \n");
                            if (usernames.Contains(accepted_user))
                            {
                                logs.AppendText("User is online, so acknowledging both sides \n");
                                if (friends.ContainsKey(incomingUsername))
                                {
                                    for (int i = 0; i < friends[incomingUsername].Count(); i++)
                                    {
                                        string off_code = "OK+" + friends[incomingUsername][i].ToString();
                                        Console.WriteLine(off_code);
                                        byte[] data = Encoding.ASCII.GetBytes(off_code);
                                        int conn_user = usernames.IndexOf(incomingUsername);
                                        foreach (Socket s in clientSockets)
                                        {
                                            if (s == clientSockets[conn_user])
                                            {
                                                s.Send(data);
                                            }
                                        }
                                    }
                                }

                                if (friends.ContainsKey(accepted_user))
                                {
                                    for (int i = 0; i < friends[accepted_user].Count(); i++)
                                    {
                                        string off_code = "OK+" + friends[accepted_user][i].ToString();
                                        byte[] data = Encoding.ASCII.GetBytes(off_code);
                                        int conn_user = usernames.IndexOf(accepted_user);
                                        foreach (Socket s in clientSockets)
                                        {
                                            if (s == clientSockets[conn_user])
                                            {
                                                s.Send(data);
                                            }
                                        }
                                    }
                                }
                            }
                            // after friend operation
                            // send the acknowledgement to the user who accepted
                            // if user that is accepted is offline
                            else
                            {
                                logs.AppendText("User is offline, so acknowledging only the one who accepts \n");
                                for (int i = 0; i < friends[incomingUsername].Count(); i++)
                                {
                                    string off_code = "OK+" + friends[incomingUsername][i].ToString();
                                    byte[] data = Encoding.ASCII.GetBytes(off_code);
                                    int conn_user = usernames.IndexOf(incomingUsername);
                                    foreach (Socket s in clientSockets)
                                    {
                                        if (s == clientSockets[conn_user])
                                        {
                                            s.Send(data);
                                        }
                                    }
                                }
                            }
                        }
                        // rejection protocol
                        else if (incomingMessage.Substring(0, start + 1) == "RJ+")
                        {
                            logs.AppendText("User rejecting protocol is activated \n");
                            string rejected_user = incomingMessage.Substring(start + 1);
                            // remove from pending list
                            pending_Dict[sent_user].Remove(rejected_user);

                            // after rejection procedure
                            // send the up to date list to the user who rejected
                            logs.AppendText("Updating the rejector's pending list \n");
                            for (int i = 0; i < pending_Dict[incomingUsername].Count(); i++)
                            {
                                string off_code = "NP+" + pending_Dict[incomingUsername][i].ToString();
                                byte[] data = Encoding.ASCII.GetBytes(off_code);
                                int conn_user = usernames.IndexOf(incomingUsername);
                                foreach (Socket s in clientSockets)
                                {
                                    if (s == clientSockets[conn_user])
                                    {
                                        s.Send(data);
                                    }
                                }
                            }
                            List<string> pender_rj = new List<string>();

                            // if rejected user is online
                            // send the information right away
                            logs.AppendText("Checking if rejected person is online... \n");
                            if (online_usercheck(rejected_user))
                            {
                                logs.AppendText("User is online, acknowledging the rejected user \n");
                                string off_code = "MS+" + sent_user;
                                byte[] data = Encoding.ASCII.GetBytes(off_code);
                                int conn_user = usernames.IndexOf(rejected_user);
                                foreach (Socket s in clientSockets)
                                {
                                    if (s == clientSockets[conn_user])
                                    {
                                        s.Send(data);
                                    }
                                }
                                logs.AppendText("User is acknowledged \n");
                            }
                            // if rejected user is offline
                            // add the message to the dictionary
                            else
                            {
                                logs.AppendText("User is not online, to acknowledge the user as soon as possible, add the info to the pending messages \n");
                                if (pending_messages.ContainsKey(rejected_user))
                                {
                                    pending_messages[rejected_user].Add(incomingUsername + " has rejected your friend request :(");
                                }
                                else
                                {
                                    pending_messages.Add(rejected_user, pender_rj);
                                    pending_messages[rejected_user].Add(incomingUsername + " has rejected your friend request :(");
                                }
                            }
                        }
                        // removing protocol
                        else if (incomingMessage.Substring(0, start + 1) == "RM+")
                        {
                            logs.AppendText("User removing protocol is activated \n");
                            List<string> pending_messages_List = new List<string>();
                            string user_tobe_removed = incomingMessage.Substring(start + 1);
                            if (friends.Count() != 0)
                            {
                                if (friends.ContainsKey(incomingUsername))
                                {
                                    if (friends[incomingUsername].Contains(user_tobe_removed))
                                    {
                                        // remove friends from both dictionary
                                        friends[incomingUsername].Remove(user_tobe_removed);
                                        friends[user_tobe_removed].Remove(incomingUsername);
                                        // send the acknowledgement with UF (unfriended) protocol
                                        string off_code = "UF+" + incomingUsername;
                                        string on_code = "UF+" + user_tobe_removed;
                                        byte[] data1 = Encoding.ASCII.GetBytes(off_code);
                                        byte[] data2 = Encoding.ASCII.GetBytes(on_code);
                                        int conn_user = usernames.IndexOf(incomingUsername);
                                        int con_user = usernames.IndexOf(user_tobe_removed);

                                        logs.AppendText("Checking if user to be removed is online \n");

                                        if (usernames.Contains(user_tobe_removed))
                                        {
                                            logs.AppendText("User to be removed is online, acknowledge the user \n");
                                            foreach (Socket s in clientSockets)
                                            {
                                                if (s == clientSockets[con_user])
                                                {
                                                    s.Send(data1);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // offline user unfriending notification operation
                                            logs.AppendText("User to be removed is offline, acknowledge the user as soon as possible by pending message list \n");
                                            if (!offline_users.Contains(user_tobe_removed))
                                            {
                                                offline_users.Add(user_tobe_removed);
                                            }
                                            pending_messages_List.Add(incomingUsername + " has removed you from friends");
                                            pending_messages.Add(user_tobe_removed, pending_messages_List);

                                        }
                                        foreach (Socket s in clientSockets)
                                        {
                                            if (s == clientSockets[conn_user])
                                            {
                                                s.Send(data2);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        logs.AppendText("User removing violation has ocurred \n");
                                        // unsuccessful operation
                                        string off_code = "UN+" + user_tobe_removed;
                                        byte[] data = Encoding.ASCII.GetBytes(off_code);
                                        int conn_user = usernames.IndexOf(incomingUsername);
                                        foreach (Socket s in clientSockets)
                                        {
                                            if (s == clientSockets[conn_user])
                                            {
                                                s.Send(data);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    logs.AppendText("User removing violation has ocurred \n");
                                    // unsuccessful operation
                                    string off_code = "UN+" + user_tobe_removed;
                                    byte[] data = Encoding.ASCII.GetBytes(off_code);
                                    int conn_user = usernames.IndexOf(incomingUsername);
                                    foreach (Socket s in clientSockets)
                                    {
                                        if (s == clientSockets[conn_user])
                                        {
                                            s.Send(data);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                logs.AppendText("User removing violation has ocurred \n");
                                // unsuccessful operation
                                string off_code = "UN+" + user_tobe_removed;
                                byte[] data = Encoding.ASCII.GetBytes(off_code);
                                int conn_user = usernames.IndexOf(incomingUsername);
                                foreach (Socket s in clientSockets)
                                {
                                    if (s == clientSockets[conn_user])
                                    {
                                        s.Send(data);
                                    }
                                }
                            }
                        }

                        else
                        {
                            logs.AppendText("deal with it \n");
                        }

                    }
                    catch
                    {

                        /*
                        if (!terminating) // disconnection
                        {
                            //logs.AppendText("A client has disconnected.\n");
                        }*/
                        thisClient.Close();
                        clientSockets.Remove(thisClient);
                        usernames.Remove(incomingUsername); // subtracting from the online usernames list
                        offline_users.Add(incomingUsername); // adding to offline usernames list
                        connected = false;
                    }
                }
            }

        }

        private int find_index(Socket thisSocket)
        {
            return clientSockets.IndexOf(thisSocket); // index of sender
        }
        private bool online_usercheck(string requested_person)
        {
            foreach (string s in usernames) // send message everyone but sender
            {
                if (s == requested_person)
                {
                    return true;
                }
            }
            return false;
        }
        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e) // crash handler
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void logs_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
