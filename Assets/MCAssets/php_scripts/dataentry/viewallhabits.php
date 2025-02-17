<?php

//local machine
/*$servername = "localhost";
$username = "root";
$password = "";
$dbname = "lz7cj-2go-u-259938";
*/
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
  habits.*,
  habit_sections.habitsection_name,
  behaviour_type.Behaviour_Description,
  behaviour_type.Behaviour_Type
FROM habits
  INNER JOIN habit_sections
    ON habits.habitsectionid = habit_sections.idhabit_sections
  INNER JOIN behaviour_type
    ON habit_sections.idhabit_sections = behaviour_type.BehaviourType_ID";
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
    <td>
      Habit ID
    </td>

      <td>
        Label
      </td>
            <td>
      Value
      </td>
            <td>
        Habit Question
      </td>
            <td>
        Min Value
      </td>
            <td>
        Max Value
      </td>
            <td>
        Yes/No
      </td>
            <td>
        Habit Section Group
      </td>
            <td>
        Behaviour Label
      </td>
            <td>
        Behaviour Description
      </td>
</tr>
<tr>
    <?php foreach($dbdata as $item): 
        echo "
        <tr><td>
        <p1><a href='newhabitsforcontent.php?contentid=" . $item['Content_ID'] . "'>" . $item['Habit_ID'] . 
        "</a></td><td>
        <p1>" . $item['label'] . 
        "</td><td>
        <p1>". $item['description'] . 
        "</td><td>
        <p1>". $item['minvalue'] . 
        "</td><td>
        <p1>". $item['maxvalue'] . 
        "</td><td>
        <p1>". $item['boolean'] . 
        "</td><td>
        <p1>". $item['habitsection_name'] . "</td>
<td>
        <p1>". $item['Behaviour_Description'] . "</td>

        <td>
        <p1>". $item['Behaviour_Type']; ?>
        	</p></td><td> 
    	<?php endforeach; ?>

</tr>

</table>
</form>
<a href="home.html">Home</a>
</body>
</html>