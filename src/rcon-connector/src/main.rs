#[macro_use]
extern crate log;

//use battlefield_rcon::rcon::RconQueryable;
use ascii::IntoAsciiString;
use battlefield_rcon::{
    bf4::{Bf4Client, Event, ServerInfoError /*, Visibility*/},
    rcon,
};
use dotenv::dotenv;
use rcon::RconResult;
use std::collections::HashMap;

use tokio_stream::StreamExt;

mod logging;

#[tokio::main]
async fn main() -> RconResult<()> {
    dotenv().ok();
    logging::init_logging();

    info!("Connecting to {:?}", dotenv::var("RCON_IPPORT").unwrap());

    let bf4 = Bf4Client::connect(
        dotenv::var("RCON_IPPORT").unwrap(),
        dotenv::var("RCON_PASSWORD")
            .unwrap()
            .into_ascii_string()
            .unwrap(),
    )
    .await
    .unwrap();

    // List players test
    // let list = bf4.list_players(Visibility::All).await.unwrap();
    // info!("{:#?}", list);

    // Server info test
    // let words = bf4.get_underlying_rcon_client().query_raw(vec!["serverInfo".into_ascii_string().unwrap()]).await.unwrap();
    // info!("serverInfo returned: {:#?}", words);

    let backend_api = match dotenv::var("BACKEND_API") {
        Ok(val) => val,
        Err(e) => panic!("could not find {}: {}", "SERVER_GUID", e),
    };

    let serverinfo = match bf4.server_info().await {
        Ok(info) => info,
        Err(ServerInfoError::Rcon(rconerr)) => return Err(rconerr),
    };
    info!("ServerName {:?}", serverinfo.server_name);

    let server_guid = match dotenv::var("SERVER_GUID") {
        Ok(val) => val,
        Err(e) => {
            warn!("could not find {}: {}", "SERVER_GUID", e);

            String::default()
        }
    };
    info!("ServerGuid {:?}", server_guid);

    let mut event_stream = bf4.event_stream().await.unwrap();
    while let Some(ev) = event_stream.next().await {
        match ev {
            Ok(Event::Authenticated { player }) => {
                info!("{} with EA GUID {} joined!", player.name, player.eaid);

                let mut map = HashMap::new();
                map.insert("serverName", format!("{}", serverinfo.server_name));
                if !(server_guid.is_empty()) {
                    map.insert("serverGuid", format!("{}", server_guid));
                }
                map.insert("eaGuid", format!("{}", player.eaid));
                map.insert("soldierName", format!("{}", player.name));

                let client = reqwest::Client::new();

                match client
                    .post(format!("{}/soldier/onjoin", backend_api))
                    .json(&map)
                    .send()
                    .await
                {
                    Ok(_) => (),
                    Err(error) => error!("Couldn't send join to backend. {:?}", error),
                }
            }
            Ok(Event::Leave {
                player,
                final_scores: _,
            }) => {
                info!("{} with EA GUID {} left!", player.name, player.eaid);

                let mut map = HashMap::new();
                map.insert("serverName", format!("{}", serverinfo.server_name));
                if !(server_guid.is_empty()) {
                    map.insert("serverGuid", format!("{}", server_guid));
                }
                map.insert("eaGuid", format!("{}", player.eaid));
                map.insert("soldierName", format!("{}", player.name));

                let client = reqwest::Client::new();

                match client
                    .post(format!("{}/soldier/onLeave", backend_api))
                    .json(&map)
                    .send()
                    .await
                {
                    Ok(_) => (),
                    Err(error) => error!("Couldn't send join to backend. {:?}", error),
                }
            }
            Ok(_) => {} // ignore other events.
            Err(_) => {
                //println!("Got error: {:?}", err);
            }
        }
    }

    info!("Graceful exit");

    Ok(())
}
