<?php
require("dbconnect.php");

//from app
 $phpname = $_POST["c_username"];
 $emailfromapp = $_POST["c_email"];

//from app
/* $phpname = "nicktest";
 $emailfromapp = "nicksamsung@beriro.co.uk";*/

//get username and password
/* $phpname = "passwordtest";
 $passwordfromapp = "passwordtest";*/

 //get hashed password from DB based on username
 
$checkuser = "SELECT users.email, users.User_ID from users where Username='" . $phpname . "' and Email='" . $emailfromapp . "'"; 
    
$result = mysqli_query($conn, $checkuser) or die("2:name check query failed");

while ($row = $result->fetch_assoc())
	{
		$dbdata[]=$row;
	}

$data = json_encode($dbdata);
foreach($dbdata as $item)
		{
				$emailreturn = $item['email']; 
		 		$userid = $item['User_ID'];
		}

		if ($userid !="")
		{
			//echo "in the positive bit";
			$code = random_str(5);
			$insertCode = "INSERT INTO passwordRecover (userid, code) VALUES ('" . $userid . "','" . $code . "' );";
				//echo $insertCode;

			mysqli_query($conn, $insertCode) or die("4");
			
			//send mail
$to = $emailfromapp;
$subject = "MasterChange Password Reset for User:" .$phpname ;
$message = "You told us that you have forgotten the password to your account on MasterChange. If this wasn't you, then please ignore this email. 
	
	To reset your password you will need the unique code: ".$code . "
	Please return to the app and using this code and your username, you will be able to reset your account password, 
	MasterChange";
$from = "passwordreset@masterchange.today";
$headers = "From: " . $from;
mail($to,$subject,$message,$headers);

echo "100";

		/*}

//echo $emailreturn;

if ($emailreturn !="")
	{*/	
			
				//echo "1";

	}
else
	{
		echo "0";
	}

function random_str(
    int $length = 64,
    string $keyspace = '0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'
): string {
    if ($length < 1) {
        throw new \RangeException("Length must be a positive integer");
    }
    $pieces = [];
    $max = mb_strlen($keyspace, '8bit') - 1;
    for ($i = 0; $i < $length; ++$i) {
        $pieces []= $keyspace[random_int(0, $max)];
    }
    return implode('', $pieces);
}

$conn->close();

?>