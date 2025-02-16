<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "lz7cj-2go-u-259938";
*/
//remote machine
//remote machine
/*$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";*/
require("dbconnect.php");

/*//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}*/

//variables from Unity
	$userid = $_POST["dbuserid"];
 	

//variables hardcoded
	// $userid = "373";
 	

$selecthabits = "Select

    behaviour_type.BehaviourType_ID

From
    behaviour_type Inner Join
    user_behaviour On user_behaviour.behaviourtypeid = behaviour_type.BehaviourType_ID
Where
    user_behaviour.userid = '" . $userid . "' And
    interested =1";

//echo $selecthabits;

 mysqli_query($conn, $selecthabits) or die("4: Get Habits Failed"); //insert data or error for failure
$result  = $conn->query($selecthabits);
if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}
$data = array("data" => $dbdata);
echo json_encode($data);




$conn->close();

?>