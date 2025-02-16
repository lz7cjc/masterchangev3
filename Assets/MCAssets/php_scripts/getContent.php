<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "masterchange";*/

//remote machine
$servername = "10.16.16.15";
$username = "mstchng";
$password = "NsNFY4Qn8";
$dbname = "mstchng";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}

 

 // $phpname = "Nickagain2";
 // $phppassword = "passwordagain2";
 // $userid = "1"



// //check if account exists
// $userid = "SELECT userid from users where Username='" . $phpname . "' AND Password='" . $phppassword; . "';";

// $namecheck = mysqli_query($conn, $namecheckquery) or die("2:name check query failed");

// if(mysqli_num_rows($namecheck) >1)
// {
//   echo "3: duplicate account; please contact support at support@masterchange.co.uk";
//   exit();
// }





//show records including new values
// $nameCheckquery = "SELECT username from users where username='" . $username	. "';";
$sqlnew = "SELECT DISTINCT
   contents.ContentTitle,
   contents.ContentBody,
   user_habits.value,
  user_habits.boolean,
  users.User_ID

FROM user_habits
       INNER JOIN users
         ON user_habits.user_id = users.User_ID,
     content_habitparameter
       INNER JOIN contents
         ON content_habitparameter.Content_ID = contents.Content_ID
WHERE users.User_ID = 1
AND ((user_habits.value >= content_habitparameter.Min_Value
and user_habits.value <= content_habitparameter.Max_Value)
OR user_habits.boolean = content_habitparameter.Boolean)";

$result = $conn->query($sqlnew);

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}

//print array in JSON format
echo json_encode($dbdata);
 // output data of each row
// $sql = "SELECT * from users";
// $result = $conn->query($sql);
// while($row = $result->fetch_assoc()) 
//    {
//         echo " - fname: " . $row["Fname"] . " -   lname: " . $row["Lname"]  . " - username: " . $row["Username"] . " - hash: " . $row["Hash"] .  " - salt: " . $row["Salt"] . "dob " . $row["DoB"] . "Email" . $row["Email"] . "<br><br>";
//    }
// // }
// } else 
//   {
//     echo "0 results";
//   }



$conn->close();

?>