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


$contenthabits = "SELECT
  contents.Content_ID,
  behaviour_type.Behaviour_Type,
  contents.ContentTitle,
  contents.ContentBody,
  behaviour_type.Behaviour_Description
FROM content_behaviours
    RIGHT OUTER JOIN  contents
    ON content_behaviours.content_id = contents.Content_ID
    LEFT OUTER JOIN behaviour_type
    ON content_behaviours.behaviourtype_id = behaviour_type.BehaviourType_ID";
    echo $contenthabits;
$result = mysqli_query($conn, $contenthabits) or die("2:name check query failed");

if ($result->num_rows > 0) 
{
   $dbdata = array();
}
while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;
}

?>
<!DOCTYPE html>
<html>
<head>
	<link rel="stylesheet" href="mystyle.css">
	<title></title>
</head>
<body >
<table border="1"> 
<tr>
	<form action="behavioursentry.php" method="post">
    <?php foreach($dbdata as $item): 
        echo "
        <tr><td>
        <p1>" . $item['Behaviour_Type'] .
        "</td><td>
        <p1><a href='behavioursentry.php?contentid=" . $item['Content_ID'] . "'>" . $item['ContentTitle'] . 
        "</a></td><td>
        <p1>" . $item['ContentBody'] . 
        "</td><td>
        <p1>". $item['Behaviour_Description'] ;?>
        </p></td><td>

	 <input type="hidden" id="contentid" name="contentid" value="<?=  $item['Content_ID'];?>">
  <input type="hidden" id="title" name="title" value="<?=  $item['ContentTitle'];?>">
   <input type="hidden" id="description" name="description" value="<?= $item['ContentBody']; ?>">
</tr>; 
    	<?php endforeach; ?>

</tr>

</table>
</form>
 <a href="home.html">Home</a>
</body>
</html>