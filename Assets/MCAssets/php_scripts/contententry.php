<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "masterchange";
*/
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

	die("Connection failed " . $conn->connect_error);

}

//store information entered in form as variables to insert into DB
$title= $_POST['title'];
$description= $_POST['Description'];
$ContentType= $_POST['typecontent'];
$wheretogo= $_POST['where'];

echo $title . "<br>" . $description;

//values into db
$insertcontent = "INSERT INTO contents (ContentTitle, ContentBody, ContentType, Generic) VALUES ('" . $title . "','" . $description . "','" . $ContentType . "','" . $wheretogo . "')";
echo $insertcontent;

mysqli_query($conn, $insertcontent) or die("4: failed to insert contents"); //insert data or error for failure
	
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

//get habits
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

//get behavious types options
$sql3= "SELECT BehaviourType_ID	, Behaviour_Type, Behaviour_Description FROM behaviour_type";
 
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

?>
<br>

<br>


<form action="contentcommit.php" method="post">

     Habits<br> 
<br>
<table>
  <tr>
    <td>
<select name='habits' size=5>
    <?php foreach($dbdata2 as $item2): ?>
        <option value="<?= $item2['Habit_ID']  ; ?>"><?= $item2['label'] . "---" . $item2['description']; ?></option>
    <?php endforeach; ?>
</select>
</td>
<td>
minvalue<br>
<input type="text" name = "minvalue" value="">  
</td>
<td>
  maxvalue<br><input type="text" name = "maxvalue" value=""> 
  </td>
  <td>Display if yes or no<br><select name="yesorno">
   <option value="">Irrelevant</option>
  <option value=1>Yes</option>
  <option value=0>No</option>
   </select>
</td>
  <td> Generic or start up <br><select name="generic">
    <option value=1>Startup</option>
   <option value=2>Everywhere</option>
  </select>
</td>

</tr>
</table>
 <input type="hidden" id="contentid" name="contentid" value="<?= $newContentID;?>">
  <input type="hidden" id="title" name="title" value="<?= $title;?>">
   <input type="hidden" id="description" name="description" value="<?= $description; ?>">

   <input type="hidden" id="ContentType" name="ContentType" value="<?= $ContentType; ?>">

 
  <br>
  Behaviours
  <br>
<select name='behaviours'  size=5>
    <?php foreach($dbdata3 as $item3): ?>
        <option value="<?= $item3['BehaviourType_ID']  ; ?>"><?= $item3['Behaviour_Type'] . "---" . $item3['Behaviour_Description']; ?></option>
   <?php endforeach; ?>  </select>

<input type="submit" name="button" value="Submit">


</form>
