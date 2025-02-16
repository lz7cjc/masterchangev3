<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "vdl54bm_lz7cj-2g";*/

//remote machine
//remote machine

$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "eBJ7bEI%zA)#Ldsc";
$dbname = "vdl54bm_lz7cj-2g";

//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("0: DB Connection failed " . $conn->connect_error);
}


 $phpname = $_POST["c_username"];
 $phppassword = $_POST["c_password"];
 $phpdob = $_POST["c_dob"];
 $phpfname = $_POST["c_fname"];
 $phplname = $_POST["c_lname"];
 $phpemail = $_POST["c_email"];
$phpemailOptin = $_POST["c_optin"];
$description = "Registration";
 $introscreen = $_POST["introscreen"];
 $switchtoVr = $_POST["switchtoVr"];
 $SkipLearning = $_POST["SkipLearning"];
 $creditgiven = "1";
$returnscene =  $_POST["returnscene"];
 $behaviour =$_POST["behaviour"];
$stage =$_POST["stage"];

 $rirosEarnt = $_POST["rirosEarnt"];
$rirosSpent = $_POST["rirosSpent"];
 $description = $_POST["description"];

//echo "stage is" . $stage . "br";

 
/*/* $phpname = "masterchange9699" . random_int(1, 1000000);
 $phppassword = "localuser";
 $phpdob = "1985";
 $phpfname = "c._fname";
 $phplname ="c._lname";
 $phpemail = "nickjklg@beriro.co.uk";
$phpemailOptin = "1";
$description = "Registration";
//  $rirosEarnt = $_POST["rirosEarnt"];
//  $rirosSpent= $_POST["rirosSpent"];
//  $rirosBought= $_POST["rirosBought"];
// $rirosBalance= $_POST["rirosBalance"];
 $introscreen = "0";
 $switchtoVr = "0";
 $SkipLearning = "1";
 $creditgiven = "1";
$returnscene = "hospital";
 $behaviour = "smoking";
$stage = "1";
 $rirosEarnt = "10000";
$rirosSpent = "250";
$description = "register";
*/


//check if name exists

$nameCheckquery = "SELECT * from users where Username='" . $phpname . "';";


$namecheck = mysqli_query($conn, $nameCheckquery) or die("2:name check query failed");

if(mysqli_num_rows($namecheck) >0)
{
  echo "3: name already exists";
  exit();
}

$dateForSQL = $phpdob;

 $hash = password_hash($phppassword, PASSWORD_DEFAULT);
 
//echo "old date " . $phpdob . " and the new date: " . $dateForSQL. "ends <br><br>";
$insertuserquery = "INSERT INTO users (Fname, Lname, Username, Hash, DoB, Email, Optin) VALUES ('" . $phpfname . "', '" . $phplname . "', '" . $phpname . "', '" . $hash . "', '" . $dateForSQL . "', '" . $phpemail . "', '" . $phpemailOptin . "' );";



//echo $insertuserquery;

mysqli_query($conn, $insertuserquery) or die("4: Registration insert record failed"); //insert data or error for failure


$user_idquery = "SELECT User_ID, Username, Fname from users where Username='" . $phpname  . "';"; 
//echo ("user id etc: " . $user_idquery);
$result = $conn->query($user_idquery);

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}

foreach($dbdata as $item)
		{
				$userid = $item['User_ID'];
		}

//insert user settings
		$insertusersettings = "INSERT INTO user_preferences(dbuserid, IntroScreen, SwitchtoVR, SkipLearningScreenInt, creditsgiven, returnToScene, stage, behaviour) VALUES ('" . $userid . "','"  . $introscreen . "','" . $switchtoVr . "','" . $SkipLearning . "','" . $creditgiven . "','" . $returnscene . "','" . $stage . "','" . $behaviour . "');";
//echo $insertusersettings;
mysqli_query($conn, $insertusersettings) or die("5: User Settings failed to update"); //insert data or error for failure


/////////////////////////////////////////////////Store Riros//////////////////////////////////////////////
$insertRiros = "INSERT INTO riros(userid, rirosEarnt, rirosSpent, description) VALUES ('" . $userid . "','"  . $rirosEarnt . "','" . $rirosSpent . "','" . $description . "');";

mysqli_query($conn, $insertRiros) or die("6: Riro Settings failed to update"); //insert data or error for failure


//send confirmation email 
$to = $phpemail;
$subject = "Welcome to The World of MasterChange, " .$phpname ;
$headers .= "BCC: registrations@masterchange.today\r\n";
$headers .= "MIME-Version: 1.0\r\n";
$headers .= "Content-Type: text/html; charset=ISO-8859-1\r\n";
$headers .= "From: support@masterchange.today";


$message = '<html><head>
<title>MasterChange Registration</title>
<meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1">
</head><body>';

$message .= "<b>Welcome to MasterChange</b><p>Thanks for registering with us; you have received your signing on bonus. If you have any problems, please reply to this email or else use support@masterchange.today. <p>

You can tell us about new behaviours you would like help with, either in the app or by email. We are building and evolving MasterChange based on what you want, so please do tell us anything you would like to see <p>

You can also let us know if you would like to be kept updated on major future releases; this is just the first version and it is only going to get better <p>

Good luck in your journey<p>
The MasterChange Team <p>
support@masterchange.today<br>
https://discord.gg/4pRNk5Ka<br>";
$message .= '<img src="http://masterchange.today/images/logo.png" alt="MasterChange Logo"><p>';
 $message .= "</body></html>";
mail($to,$subject,$message,$headers);


echo '{"UserID":"' . $userid .  '", "Username":"' . $phpname  . '"}';



$conn->close();

?>