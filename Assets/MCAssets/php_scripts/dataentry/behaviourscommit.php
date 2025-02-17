<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "lz7cj-2go-u-259938";*/

//remote machine
$servername = "10.16.16.15";
$username = "lz7cj-2go-u-259938";
$password = "NsNFY4Qn8";
$dbname = "lz7cj-2go-u-259938";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}

//store information entered in form as variables to insert into DB
$title= $_POST['ContentTitle'];
$description= $_POST['ContentBody'];
$contentid= $_POST['Contentid'];
$behaviours= $_POST['behaviours'];

echo $title . "<br>" . $description;

//values into db
$insertbehaviour = "INSERT INTO content_behaviours (content_id, behaviourtype_id)
  VALUES('" . $contentid . "','" . $behaviours  . "')";
echo $insertbehaviour;

mysqli_query($conn, $insertbehaviour) or die("4: failed to insert contents"); //insert data or error for failure
	
	$getcontentid = "Select Content_ID from contents where ContentTitle ='" . $title . "'";

$result = $conn->query($getcontentid);

if ($result->num_rows > 0) {
  // output data of each row
  while($row = $result->fetch_assoc()) {
  	$newContentID = $row["Content_ID"];
   //  echo "content id: " . $row["Content_ID"];
   // echo "newContentID id: " . $newContentID;
  }
} else {
  echo "0 results";
}

///*get habits
/*$sql2= "SELECT Habit_ID, label, description FROM habits";
 
//Prepare the select statement.
$result2 = mysqli_query($conn, $sql2) or die("2:name check query failed");

if ($result2->num_rows > 0) 
{
   $dbdata2 = array();
}
while ($row = $result2->fetch_assoc())
{
$dbdata2[]=$row;

}*/

//get behavious types options
/*$sql3= "SELECT BehaviourType_ID	, Behaviour_Type, Behaviour_Description FROM behaviour_type";
 
//Prepare the select statement.
$result3 = mysqli_query($conn, $sql3) or die("2:name check query failed");

if ($result3->num_rows > 0) 
{
   $dbdata3 = array();
}
while ($row = $result3->fetch_assoc())
{
$dbdata3[]=$row;

}*/

header("Location: contentviewbehaviours.php");
exit();

?>
<br>
  <a href="home.html">Home</a> ||||| <a href="contentviewbehaviours.php">View Behaviours</a>
<br>


