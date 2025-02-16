<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "masterchange";*/

//remote machine
$servername = "localhost";
$username = "vdl54bm_lz7cj-2go-u-259938";
$password = "eBJ7bEI%zA)#Ldsc";
$dbname = "vdl54bm_lz7cj-2g";


//create connection
$conn = new mysqli($servername, $username, $password, $dbname);

if ($conn->connect_error) 
{

	die("Connection failed " . $conn->connect_error);

}


//get dropdowns
//Our select statement. This will retrieve the data that we want.

//get content type
$sql = "SELECT ContentType_ID, ContentName FROM content_type";
 echo "SELECT ContentType_ID, ContentName FROM content_type" . $sql;
//Prepare the select statement.
$result = $conn->query($sql);
if ($result->num_rows > 0) 
{
   $dbdata = array();
}

while ($row = $result->fetch_assoc())
{
$dbdata[]=$row;

}

//get generic options
$sql1 = "SELECT * FROM generic_options";
  echo "SELECT * FROM generic_options" . $sql1;

//Prepare the select statement.
$result1 = $conn->query($sql1);

if ($result1->num_rows > 0) 
{
   $dbdata1 = array();
}
while ($row = $result1->fetch_assoc())
{
$dbdata1[]=$row;

}
/*
//get habits
$sql2= "SELECT Habit_ID, label, description FROM habits";
 echo "SELECT Habit_ID, label, description FROM habits" . $sql2;

//Prepare the select statement.
$result2 = mysqli_query($conn, $sql2) or die("2:name check query failed");

if ($result2->num_rows > 0) 
{
   $dbdata2 = array();
}
while ($row2 = $result2->fetch_assoc())
{
$dbdata2[]=$row2;

}

//get behavious types options
$sql3= "SELECT BehaviourType_ID	, Behaviour_Type, Behaviour_Description FROM behaviour_type";
  echo "SELECT BehaviourType_ID , Behaviour_Type, Behaviour_Description FROM behaviour_type" . $sql3;

//Prepare the select statement.
$result3 = mysqli_query($conn, $sql3) or die("2:name check query failed");

if ($result3->num_rows > 0) 
{
   $dbdata3 = array();
}
while ($row3 = $result3->fetch_assoc())
{
$dbdata3[]=$row3;

}


$conn->close();
*/
?>
<html>
<head>
  <style type="text/css">
body {
font-size: 12px;
}
h1 {
font-size: 1em;
line-height: 1.5;
font-weight: bold;
margin: 1em 0 0 0;
}
p {
margin: 0;
width: 33em;
}
p.one {
font-size: 18px;
line-height: 1.5em;
font-size: 1em;

font-weight: bold;

}
p.two {
font-size: 1.2em;
line-height: 1.5em;
}
</style>
</head>
<body>
will this appear?
<form action="contententry.php" method="post">
<p class="one">Title </p>
  <br>
    <input type="text" id="title" name="title" style="width: 60%; height: 50px"; 'font-size: 32px;' >
    <br>
    Description
    <br>
<textarea name="Description" rows="10" cols="50"></textarea>
<br>
<select name="typecontent">
    <?php foreach($dbdata as $item): ?>
        <option value="<?= $item['ContentType_ID']; ?>"><?= $item['ContentName']; ?></option>
    <?php endforeach; ?>

        Habits<br> </select>
<select name="where">
    <?php foreach($dbdata1 as $item1): ?>
        <option value="<?= $item1['generic_optionsid']; ?>"><?= $item1['label']; ?></option>
    <?php endforeach; ?>

</select>
<br>


 <input type="submit" name="button" value="Submit">
</form> 

</body>
</html>

