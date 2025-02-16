<?php

//local machine
$servername = "localhost";
$username = "root";
$password = "";
$dbname = "vdl54bm_lz7cj-2g";

//remote machine
/*$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "eBJ7bEI%zA)#Ldsc";
$dbname = "vdl54bm_lz7cj-2g";
*/

//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

//check connection
if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}

//store information entered in form as variables to insert into DB
$contentid = $_POST['contentid'];
$title= $_POST['title'];
$description= $_POST['description'];
$ContentType= $_POST['ContentType'];
//$wheretogo= $_POST['where'];
$habitid = $_POST['habits'];
$minvalue = $_POST['minvalue'];
$maxvalue = $_POST['maxvalue'];
$boolvalue = $_POST['yesorno'];
$generic = $_POST['generic'];
$habitid = $_POST['habits'];
$behavioursid = $_POST['behaviours'];


//echo $behaviours[];
echo $title . "<br>" . $description;

//values into db
        //habits
$insertcontenthabits = "INSERT INTO content_habitparameter (Content_ID, Habit_ID, Min_Value, Max_Value, yesorno, Section_Generic)
VALUES ('" . $contentid . "','" . $habitid . "','" . $minvalue . "','" . $maxvalue . "','" . $boolvalue . "','" . $generic  .  "')";

echo "insertcontenthabits:" . $insertcontenthabits;
mysqli_query($conn, $insertcontenthabits) or die("4: failed to insert content habits"); //insert data or error for failure

  
//behaviour
$insertcontentbehaviour = "INSERT INTO content_behaviours (content_id, behaviourtype_id)
VALUES ('" . $contentid . "','" . $behavioursid   .  "')";

echo "insertcontenthabits: " . $insertcontentbehaviour;
mysqli_query($conn, $insertcontentbehaviour) or die("4: failed to insert content habits"); //insert data or error for failure
  


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

/*//get habits
$sql2= "SELECT Habit_ID, label, description FROM habits";
 
//Prepare the select statement.
$result2 = mysqli_query($conn, $sql2) or die("2:name check query failed");

if ($result2->num_rows > 0) 
{
   $dbdata2 = array();
}
while ($row = $result2->fetch_assoc())
{
$dbdata2[]=$row;

}
*/
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

}
//var_dump($dbdata1);

*/
?>
<!DOCTYPE html>
<html>
<head>
  <title></title>
</head>
<body>

<br>
<!-- <form action="behavioursentry.php" method="post">
habitid: <br>
<input type="text" id="habitid" name = "habitid"> 
<br>minvalue <br>
<input type="text" id="minvalue" name = "minvalue">  Max Value<input type="text" name = "maxvalue" > Yes or no  <input type="text" name = "boolvalue">Startup or everywhere <input type="text" name = "generic"> 
<br>
Behaviour type
<input type="text" id="behaviourid" name = "behaviourid" > 
 <input type="hidden" id="contentid" name="contentid" value="<?= $newContentID;?>">
  <input type="hidden" id="title" name="title" value="<?= $title;?>">
   <input type="hidden" id="description" name="description" value="<?= $description; ?>">
 <input type="submit" name="button" value="Submit">
</form>  -->
<br>
<!-- <table border="1"> 
<tr>
	<td>
    <?php foreach($dbdata2 as $item2): 
        echo "<tr><td>" . $item2['Habit_ID'] ."</td><td>" . $item2['label'] . "</td><td>" . $item2['description'] . "</td></tr>"; 
    	endforeach; ?>
</td>

	<td>

<?php foreach($dbdata3 as $item3): 
        echo "<tr><td>" . $item3['BehaviourType_ID'] ."</td><td>" . $item3['Behaviour_Type'] . "</td><td>" . $item3['Behaviour_Description'] . "</td></tr> "; 
    	endforeach; ?>


	</td>
</tr>

</table> -->

<a href="home.html"> Home</a>
</body>
</html>

