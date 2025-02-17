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
$contentid = $_GET['contentid'];



$sql_contents= "SELECT
  *
FROM contents where contents.Content_ID = " . $contentid;

$result = mysqli_query($conn, $sql_contents) or die("2:name check query failed");
if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;

}

foreach($dbdata as $item):
      echo "<p3>Title</p3><br><p2>" . $item['ContentTitle'] . "</p2><br><p3>Description</p3><br><p2>" . $item['ContentBody'] . "</p2>" ;
       $ContentTitle = $item['ContentTitle'];
       $ContentBody = $item['ContentBody'];
    

 endforeach;


//get behavious types options
$sql3= "SELECT BehaviourType_ID , Behaviour_Type, Behaviour_Description FROM behaviour_type";
 
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
//get behavious types already entered for this piece of content
$sql4= "SELECT
  behaviour_type.Behaviour_Type
FROM content_behaviours
  INNER JOIN behaviour_type
    ON content_behaviours.behaviourtype_id = behaviour_type.BehaviourType_ID
WHERE content_behaviours.content_id = '" . $contentid . "'";
// echo $sql4;
//Prepare the select statement.
$result4 = mysqli_query($conn, $sql4) or die("2:name check query failed");

if ($result4->num_rows > 0) 
{
   $dbdata4 = array();
}
while ($row = $result4->fetch_assoc())
{
$dbdata4[]=$row;

}
echo "<br><p3>Behaviours</p3>";
foreach($dbdata4 as $item):
      echo "<br><p2>" . $item['Behaviour_Type'] . "</p2>" ;
      $behaviour = $item['Behaviour_Type'];

 endforeach;


?>
<!DOCTYPE html>
<html>
<head>
  <link rel="stylesheet" href="mystyle.css">
  <title></title>
</head>
<body >


<form action="behaviourscommit.php" method="post">

<select name='behaviours' size=16>
    <?php foreach($dbdata3 as $item3): ?>
        <option value="<?= $item3['BehaviourType_ID']  ; ?>"><?= $item3['Behaviour_Type'] . "---" . $item3['Behaviour_Description']; ?></option>
   <?php endforeach; ?>  </select>
 <input type="hidden" name="Contentid" value="<?php echo $contentid ?>">
 <input type="hidden" name="ContentTitle" value="<?php echo $ContentTitle ?>">
 <input type="hidden" name="ContentBody" value="<?php echo $ContentBody ?>">
 <input type="submit" name="commit" value="Submit">
</form>
<p>
  <a href="home.html">Home</a>
</body>
</html>
